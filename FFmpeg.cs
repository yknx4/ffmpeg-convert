using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ffmpeg_convert
{
    public delegate void ProgressChangedEventHandler(object sender, FFmpeg.Convert.ProgressChangedEventArgs e);

    /// <summary>
    ///  FFmpeg class
    /// </summary>
    public class FFmpeg
    {
        /// <summary>
        /// FFmpeg supported architectures
        /// </summary>
        

        
        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpeg"/> class.
        /// </summary>
        /// <param name="binary">The ffmpeg binary root path.</param>
        /// <param name="arch">The architecture of the system (Will load from a folder with the same name at binary troot path).</param>
        public FFmpeg(FileInfo binary)
        {
            Properties.ffmpeg.Default.bin_location = binary.FullName;
           
            Start();
        }
        private string arch;
        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpeg"/> class.
        /// </summary>
        public FFmpeg()
        {
            Start();
        }

        /// <summary>
        /// Gets the default converter.
        /// </summary>
        /// <value>
        /// The converter.
        /// </value>
        public Convert Converter
        {
            get;
            private set;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <exception cref="System.ArgumentException">ffmpeg cannot be found in the specified path: +finalPath.FullName;path</exception>
        private void Start()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                arch = "x64";
            }
            else
            {
                arch = "x86";
            }
            FileInfo current_dir = new FileInfo(System.Uri.UnescapeDataString(new System.Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath));
            
            string full_path = Path.Combine(current_dir.DirectoryName,"external",arch,"ffmpeg.exe") ;
            FileInfo finalPath = new FileInfo(full_path);
            if (!finalPath.Exists)
            {
                throw new ArgumentException("ffmpeg cannot be found in the specified path: " + finalPath.FullName, "path");
            }
            Converter = new Convert();
            Convert.Path = finalPath.FullName;
        }

        /// <summary>
        /// Argumets used at an Mp3Conversion
        /// </summary>
        public class Mp3ConversionArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Mp3ConversionArgs"/> class.
            /// </summary>
            public Mp3ConversionArgs()
            {
                isVariable = true;
                _preset = 2;
                MinBitrate = 0;
            }

            /// <summary>
            /// Gets or sets a value indicating whether output bitrate [is variable].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [is variable]; otherwise, <c>false</c>.
            /// </value>
            public bool isVariable { get; set; }

            private int _preset;

            public int MinBitrate { get; set; }

            /// <summary>
            /// Gets or sets the preset bitrate.
            /// </summary>
            /// <value>
            /// The preset.
            /// </value>
            /// <exception cref="System.ArgumentOutOfRangeException">
            /// Preset;VBR preset must be within 0 and 9
            /// or
            /// Preset;CBR preset must be within 8kbps and 320kpbs (Also multiple of 8)
            /// </exception>
            public int Preset
            {
                get
                {
                    return _preset;
                }
                set
                {
                    if (isVariable)
                    {
                        if (value >= 0 && value <= 9)
                        {
                            _preset = value;
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException("Preset", "VBR preset must be within 0 and 9");
                        }
                    }
                    else
                    {
                        if (value >= 8 && value <= 320 && value % 8 == 0)
                        {
                            _preset = value;
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException("Preset", "CBR preset must be within 8kbps and 320kpbs (Also multiple of 8)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Class to perform conversions
        /// </summary>
        ///
        public class Convert
        {
            /// <summary>
            /// Gets or sets the conversion path.
            /// </summary>
            /// <value>
            /// The path.
            /// </value>
            public static string Path { get; set; }

            /// <summary>
            /// Occurs when [progress changed].
            /// </summary>
            public event ProgressChangedEventHandler ProgressChanged;

            /// <summary>
            /// Event args for [progress changed]
            /// </summary>
            public class ProgressChangedEventArgs : EventArgs
            {
                public Double Progress { get; set; }
            }

            /// <summary>
            /// Raises the <see cref="E:ProgressChanged" /> event.
            /// </summary>
            /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
            protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
            {
                if (ProgressChanged != null)
                    ProgressChanged(this, e);
            }

            private TagLib.File currentItem;

            /// <summary>
            /// To the MP3.
            /// </summary>
            /// <param name="input">The input.</param>
            /// <param name="output">The output.</param>
            /// <param name="args">The arguments.</param>
            /// <returns>True if conversion was succesfull, False otherwise writing error to LastError property</returns>
            public bool ToMp3(FileInfo input, FileInfo output, Mp3ConversionArgs args)
            {
                
                currentItem = TagLib.File.Create(input.FullName);
                if (input.Extension.ToLower() == ".mp3" && (args.MinBitrate > 0 && currentItem.Properties.AudioBitrate < args.MinBitrate))
                {
                    input.CopyTo(output.FullName, true);
                    return true;
                }
                ProcessStartInfo startInfo = new ProcessStartInfo();
                Process mProcess = new Process();
                mProcess.EnableRaisingEvents = true;
                mProcess.ErrorDataReceived += handleProgressOutput;
                mProcess.OutputDataReceived += handleProgressOutput;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardError = true;
                startInfo.FileName = Path;
                startInfo.Arguments = "";
                startInfo.Arguments += "-i \"" + input.FullName + "\"";
                startInfo.Arguments += " -codec:a libmp3lame";
                if (args.isVariable)
                {
                    startInfo.Arguments += " -q:a " + args.Preset;
                }
                else
                {
                    startInfo.Arguments += " -b " + args.Preset;
                }
                startInfo.Arguments += " \"" + output.FullName + "\"";
                startInfo.Arguments += " -vsync 2 -y -loglevel fatal -stats";
                mProcess.StartInfo = startInfo;
                //Console.WriteLine(Environment.NewLine + "Args: " + startInfo.Arguments + Environment.NewLine);
                if (mProcess.Start())
                {
                    LastError = "Args:" + startInfo.Arguments + Environment.NewLine;
                    mProcess.BeginOutputReadLine();
                    mProcess.BeginErrorReadLine();
                    mProcess.WaitForExit();
                    
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Handles the progress output.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
            private void handleProgressOutput(object sender, DataReceivedEventArgs e)
            {
                LastError += e.Data + Environment.NewLine;
                ParseProgressLine(e.Data);
            }

            /// <summary>
            /// Parses the progress line in FFmpeg output.
            /// </summary>
            /// <param name="text">The text from output.</param>
            private void ParseProgressLine(string text)
            {
                string original;
                if (text == null) original = string.Empty;
                else original = text;
                //Console.WriteLine("DEB: " + original);
                if (original.Contains("time="))
                {
                    string timespan = original.Substring(original.IndexOf("time=") + 5, 11);
                    TimeSpan mPosicion;
                    TimeSpan.TryParse(timespan, out mPosicion);
                    mPosicion = mPosicion.ZeroMilliseconds();
                    double progress = mPosicion.ZeroMilliseconds().TotalSeconds / currentItem.Properties.Duration.ZeroMilliseconds().TotalSeconds;
                    OnProgressChanged(new ProgressChangedEventArgs { Progress = progress });

                    //Console.WriteLine(mPosicion.ToString() + " / "+currentItem.Properties.Duration.ZeroMilliseconds().ToString()+"    "+(progress*100).ToString()+"%");
                }
            }

            public string LastError { get; set; }
        }
    }
}