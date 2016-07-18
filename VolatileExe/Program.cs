namespace VolatileExe
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();

            string handle = GetPipeHandle(args);

            if (handle == null)
                return;
            

            List<byte> file = new List<byte>();
            List<byte> b64c = new List<byte>();

            using (AnonymousPipeClientStream pipe = new AnonymousPipeClientStream(PipeDirection.In, handle))
            {
#if DEBUG
                Thread.Sleep(9000);
#endif
                byte b;

                while ((b = (byte)pipe.ReadByte()) != 0xff)
                {
                    file.Add(b);
                }

                while ((b = (byte)pipe.ReadByte()) != 0xff)
                {
                    b64c.Add(b);
                }
            }

            if (file.Count > 1 && b64c.Count > 1)
                WriteToExe(Encoding.UTF8.GetString(file.ToArray()), b64c.ToArray());
        }

        static void WriteToExe(string file, byte[] b64)
        {
            int tries = 0;
            while (tries < 10)
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                    {
                        fs.Lock(0, fs.Length);
                        fs.Seek(0, SeekOrigin.Begin);

                        string data = String.Empty;
                        byte[] bytes = new byte[4];

                        while (!data.StartsWith("64[[") && fs.Length > fs.Position)
                        {
                            fs.Read(bytes, 0, 4);
                            data = Encoding.UTF8.GetString(bytes);
                        }

                        if (data == "64[[")
                            fs.Write(b64, 0, b64.Length);

                        fs.Unlock(0, fs.Length);
                    }

                    break;
                }
                catch (Exception)
                {
                    tries++;
                    Thread.Sleep(50 * tries);
                }
            }
        }

        static string GetPipeHandle(string[] args)
        {
            return args.FirstOrDefault(x => x.All(c => char.IsDigit(c)));
        }
    }
}
