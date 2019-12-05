using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Grail
{

    public class CommandResult
    {
        public string Output { get; set; }
        public string Errors { get; set; }
        public int ExitCode { get; set; }
    }

    public static class CommandExecutor
    {

        public static CommandResult RunCommand(string fileName, string args, string workingDirectory = null)
        {
            if (workingDirectory == null) workingDirectory = Environment.CurrentDirectory;

            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = false
            })
            {
                Debug.WriteLine($"Running Command: {fileName} {args}");
                Debug.WriteLine($"Start: {DateTime.Now}");
                bool started = process.Start();
                if (!started)
                {
                    throw new InvalidOperationException("Could not start process: " + process);
                }

                var result = new CommandResult();

                // ReSharper disable once AccessToDisposedClosure
                var errorTask = Task.Run(() => { result.Errors = process.StandardError.ReadToEnd(); });

                // ReSharper disable once AccessToDisposedClosure
                var outputTask = Task.Run(() => { result.Output = process.StandardOutput.ReadToEnd(); });

                process.WaitForExit();
                outputTask.Wait();
                errorTask.Wait();

                result.ExitCode = process.ExitCode;

                Debug.WriteLine($"End: {DateTime.Now}");
                return result;
            }
        }



        public class CommandResultLines
        {
            readonly ReaderWriterLockSlim outputLocker;
            readonly ReaderWriterLockSlim errorsLocker;

            public List<string> Output { get; }
            public List<string> Errors { get; }
            public int ExitCode { get; set; }

            public void AddOutput(string line)
            {
                try
                {
                    outputLocker.EnterWriteLock();
                    Output.Add(line);
                }
                finally
                {
                    outputLocker.ExitWriteLock();
                }
            }

            public void AddError(string line)
            {
                try
                {
                    errorsLocker.EnterWriteLock();
                    Errors.Add(line);
                }
                finally
                {
                    errorsLocker.ExitWriteLock();
                }
            }

            /// <inheritdoc />
            public CommandResultLines()
            {
                outputLocker = new ReaderWriterLockSlim();
                errorsLocker = new ReaderWriterLockSlim();
                try
                {
                    outputLocker.EnterWriteLock();
                    errorsLocker.EnterWriteLock();
                    Output = new List<string>();
                    Errors = new List<string>();
                }
                finally
                {
                    outputLocker.ExitWriteLock();
                    errorsLocker.ExitWriteLock();
                }
            }
        }

        public static class CommandExecutorLines
        {
            public static CommandResultLines RunCommand(string fileName, string args, string workingDirectory = null)
            {
                if (workingDirectory == null) workingDirectory = Environment.CurrentDirectory;

                using (var process = new Process
                {
                    StartInfo =
                    {
                        FileName = fileName,
                        Arguments = args,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true,

                })
                {
                    var result = new CommandResultLines();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) return;
                        //Debug.WriteLine(e.Data);
                        result.AddOutput(e.Data);
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) return;
                        //Debug.WriteLine(e.Data);
                        result.AddError(e.Data);
                    };
                    process.Exited += (sender, e) =>
                    {
                        //((Process)sender).WaitForExit();
                        //Debug.WriteLine("Exited (Event)");
                    };

                    Debug.WriteLine($"START: {fileName} {args}");
                    bool started = process.Start();
                    if (!started) throw new InvalidOperationException("Could not start process: " + process);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var processExited = process.WaitForExit(PROCESS_TIMEOUT);

                    if (!processExited)
                    {
                        process.Kill();
                        throw new Exception("ERROR: Process took too long to finish");
                    }

                    Thread.Sleep(1000); // Out last few output lines can get lost
                    result.ExitCode = process.ExitCode;

                    return result;
                }
            }

            private const int PROCESS_TIMEOUT = 20 * 60 * 1000;
        }
    }
}
