using System;
using System.IO;

const string file = @"VolatileExe";
File.WriteAllText(file + ".txt", Convert.ToBase64String(File.ReadAllBytes(file + ".exe")));