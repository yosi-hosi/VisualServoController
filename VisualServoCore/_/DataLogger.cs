using System;
using System.IO;
using System.Text.Json;
using OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;

namespace VisualServoCore
{
    public class DataLogger<T> : IDisposable
    {

        private readonly string _name;
        private BgrXyzRecorder _dwrt;
        private StreamWriter _sw;

        public DataLogger(Size? size)
        {
            var t = DateTimeOffset.Now;
            _name = $"{t.Year}{t.Month:d2}{t.Day:d2}{t.Hour:d2}{t.Minute:d2}{t.Second}";
            if (!Directory.Exists("log")) Directory.CreateDirectory("log");
            Directory.CreateDirectory("log\\" + _name);
            _sw = new($"log\\{_name}\\{_name}.json");
            if (size is not null)
                _dwrt = new($"log\\{_name}\\{_name}.yms");
        }

        public void Write(LogObject<T> data)
        {
            if (_sw?.BaseStream is not null)
                _sw.WriteLine(JsonSerializer.Serialize(data));
        }

        public void Write(BgrXyzMat frame)
        {
            if (_dwrt is not null)
                _dwrt.WriteFrame(frame);
        }

        public void Dispose()
        {
            _sw?.Dispose();
            _dwrt?.Dispose();
            _sw = null;
            _dwrt = null;
        }

    }
}
