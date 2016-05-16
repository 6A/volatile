
/*
    Single-file class made to save data in the executable.


    ----

    Copyright (c) 2016 idotjee

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

namespace Volatile
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Diagnostics;
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class VolatileObjectAttribute : Attribute
    {
        public VolatileObjectAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class VolatileObjectMemberAttribute : Attribute
    {
        public byte Index { get; private set; }

        public VolatileObjectMemberAttribute(byte index)
        {
            Index = index;
        }
    }

    public sealed class VolatileContext<T> : IDisposable, IEnumerable<T>
    {
        private static readonly byte[] Salt = new byte[] { 86, 14, 47, 23, 84, 96, 2, 9 };
        
        private List<T> objects;
        private string file;
        private string password;

        private ConstructorInfo ctor;
        private VolatileObjectAttribute attr;
        private List<Tuple<Type, VolatileObjectMemberAttribute, MemberInfo>> members;

        public VolatileContext(string key)
        {
            object[] attrs = typeof(T).GetCustomAttributes(typeof(VolatileObjectAttribute), false);

            if (attrs.Length != 1)
                throw new Exception("Class must have a VolatileObjectAttribute");

            attr = attrs[0] as VolatileObjectAttribute;

            members = (from member in typeof(T).GetMembers()
                       let attributes = member.GetCustomAttributes(typeof(VolatileObjectMemberAttribute), false)
                       let type =
                          (member is FieldInfo) ? (member as FieldInfo).FieldType
                        : (member is MethodInfo) ? (member as MethodInfo).ReturnType
                        : (member is PropertyInfo) ? (member as PropertyInfo).PropertyType
                        : null
                       where attributes.Length == 1 && type != null
                       select new Tuple<Type, VolatileObjectMemberAttribute, MemberInfo>(type, attributes[0] as VolatileObjectMemberAttribute, member))
                       .ToList();

            ctor = typeof(T).GetConstructor(members.Select(x => x.Item1).ToArray());
            if (ctor == null)
                throw new Exception("No valid constructor present on type '" + typeof(T) + "'.");

            objects = new List<T>();
            password = key;

            file = Assembly.GetExecutingAssembly().Location;
            load();
        }

        public void Add(T obj)
        {
            objects.Add(obj);
        }

        public void Remove(T obj)
        {
            objects.Remove(obj);
        }

        private byte[] getDataBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                foreach (T obj in objects)
                {
                    Dictionary<int, string> values = new Dictionary<int, string>();

                    foreach (var i in members)
                    {
                        string v = String.Empty;

                        if (i.Item3 is PropertyInfo)
                            v = (i.Item3 as PropertyInfo).GetGetMethod(true).Invoke(obj, new object[0])?.ToString();
                        else if (i.Item3 is FieldInfo)
                            v = (i.Item3 as FieldInfo).GetValue(obj)?.ToString();
                        else if (i.Item3 is MethodInfo)
                            v = (i.Item3 as MethodInfo).Invoke(obj, new object[(i.Item3 as MethodInfo).GetParameters().Length])?.ToString();

                        values.Add(i.Item2.Index, v);
                    }

                    foreach (var mbr in values.OrderBy(x => x.Key))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(mbr.Value ?? String.Empty);
                        ms.WriteByte(0xfe);
                        ms.Write(data, 0, data.Length);
                    }
                    ms.WriteByte(0xff);
                }

                if (ms.Length == 0)
                    return new byte[0];
                return ms.ToArray();
            }
        }

        private void initialize(byte[] data)
        {
            object[] parameters = new object[members.Count];

            int index = 0;
            int i = 0;

            while (i < data.Length)
            {
                byte b = data[i];

                if (b == 0xff)
                {
                    objects.Add((T)ctor.Invoke(parameters));
                    if (i == data.Length - 1) return;

                    parameters = new object[members.Count];
                }
                else if (b == 0xfe)
                {
                    var val = data.Skip(++i).TakeWhile(x => x < 0xfe && i++ < data.Length).ToArray();
                    if (val.Length != 0)
                        parameters[index] = Encoding.UTF8.GetString(val);
                    index++;
                }
                else
                {
                    i++;
                }
            }
        }

        // using http://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
        private void save()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    var key = new Rfc2898DeriveBytes(password, Salt, 1000);

                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] dataBytes = getDataBytes();
                        cs.Write(dataBytes, 0, dataBytes.Length);
                    }
                }

                byte[] msBytes = ms.ToArray();
                byte[] finale = new byte[msBytes.Length + 4];

                BitConverter.GetBytes(msBytes.Length).CopyTo(finale, 0);
                msBytes.CopyTo(finale, 4);

                SaveTo(file, finale);
            }
        }

        // using http://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
        private void load()
        {
            using (UnmanagedMemoryStream content = DATA.Base64Content)
            {
                byte[] lengthBytes = new byte[4];

                content.Seek(4, SeekOrigin.Begin);
                content.Read(lengthBytes, 0, 4);

                if (lengthBytes.All(x => x == 65))
                    return;

                byte[] allBytes = new byte[BitConverter.ToInt32(lengthBytes, 0)];
                content.Read(allBytes, 0, allBytes.Length);
                
                var key = new Rfc2898DeriveBytes(password, Salt, 1000);

                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(allBytes, 0, allBytes.Length);
                        }

                        initialize(ms.ToArray());
                    }
                }
            }
        }

        public void Dispose()
        {
            save();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static void SaveTo(string file, byte[] data)
        {
            string temp = Path.Combine(Path.GetTempPath(), "saver.exe");

            using (UnmanagedMemoryStream ms = DATA.Base64Exe)
            using (FileStream fs = File.Create(temp))
            {
                ms.CopyTo(fs);
            }

            byte[] fileLocationBytes = Encoding.UTF8.GetBytes(file);

            using (AnonymousPipeServerStream pipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            {
                Process.Start(new ProcessStartInfo(temp, pipe.GetClientHandleAsString())
                {
                    UseShellExecute = false
                });

                pipe.DisposeLocalCopyOfClientHandle();

                pipe.Write(fileLocationBytes, 0, fileLocationBytes.Length);
                pipe.WriteByte(0xff);
                pipe.WaitForPipeDrain();

                pipe.Write(data, 0, data.Length);
                pipe.WriteByte(0xff);

                try
                {
                    pipe.WaitForPipeDrain();
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
