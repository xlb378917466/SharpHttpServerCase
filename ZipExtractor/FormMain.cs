﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Forms;
using ZipExtractor.Properties;

namespace ZipExtractor
{
    public partial class FormMain : Form
    {
        private BackgroundWorker _backgroundWorker;
        static EventWaitHandle autoUpdateHappenedEvent = EventWaitHandle.OpenExisting("E8794D2D446C430DAF584BD989BCB6BFAutoUpdateHappened");
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 3)
            {
                //解压升级包的临时目录
                var tempDirectory = args[1];
                ///主程序完整路径
                var main_exeFullName = args[2];
                //检查是否需要重新启动
                var needRestart = "true".Equals(args[3], StringComparison.InvariantCultureIgnoreCase);
                if (needRestart)
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            if (process.MainModule.FileName.Equals(main_exeFullName))
                            {
                                labelInformation.Text = @"等待主程序退出...";
                                process.WaitForExit();
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                        }
                    }
                }

                // Extract all the files.
                _backgroundWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };

                _backgroundWorker.DoWork += (o, eventArgs) =>
                {
                    try
                    {
                        labelInformation.Text = @"开始备份程序文件...";
                        var path = Path.GetDirectoryName(main_exeFullName);

                        #region 更新之前备份TechSVR的程序文件
                        var bakDirectory = Path.Combine(path, Constants.Bak_TechSvr + DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒ffff"));
                        if (!Directory.Exists(bakDirectory))
                        {
                            Directory.CreateDirectory(bakDirectory);

                            foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                            {
                                if (!file.Contains(Constants.Bak_TechSvr))
                                {

                                    var destFile = file.Replace(path, bakDirectory);
                                    var destDirectory = Path.GetDirectoryName(destFile);
                                    if (!Directory.Exists(destDirectory))
                                    {
                                        Directory.CreateDirectory(destDirectory);
                                    }
                                    File.Copy(file, destFile, true);
                                }
                            }
                        }
                        #endregion


                        var isContainDll = false;
                        labelInformation.Text = @"开始安装升级程序包...";
                        #region 解压升级包到主程序的安装目录
                        // Open an existing zip file for reading.
                        using (ZipStorer zip = ZipStorer.Open(tempDirectory, FileAccess.Read))
                        {
                            // Read the central directory collection.
                            List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                            for (var index = 0; index < dir.Count; index++)
                            {
                                if (_backgroundWorker.CancellationPending)
                                {
                                    eventArgs.Cancel = true;
                                    zip.Close();
                                    return;
                                }
                                ZipStorer.ZipFileEntry entry = dir[index];

                                var filename = Path.Combine(path, entry.FilenameInZip);
                                isContainDll = filename.EndsWith(".dll");
                                zip.ExtractFile(entry, filename);
                                _backgroundWorker.ReportProgress((index + 1) * 100 / dir.Count, string.Format(Resources.CurrentFileExtracting, entry.FilenameInZip));
                            }
                        }
                        #endregion

                        //如果升级的文件中包含DLL 且没有要求重启主程序，择触发自动升级事件，让程序重新加载插件数据
                        if (isContainDll && !needRestart)
                        {
                            autoUpdateHappenedEvent.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        labelInformation.Text = @"安装升级程序包出错...";
                        MessageBox.Show("安装出错," + ex.ToString());
                    }
                };

                _backgroundWorker.ProgressChanged += (o, eventArgs) =>
                {
                    progressBar.Value = eventArgs.ProgressPercentage;
                    labelInformation.Text = eventArgs.UserState.ToString();
                };

                _backgroundWorker.RunWorkerCompleted += (o, eventArgs) =>
                {
                    if (!eventArgs.Cancelled)
                    {
                        labelInformation.Text = @"Finished";
                        try
                        {   //如果需要重新启动主程序，则由此开始启动程序
                            if (needRestart)
                            {
                                ProcessStartInfo processStartInfo = new ProcessStartInfo(main_exeFullName);
                                if (args.Length > 4)
                                {
                                    processStartInfo.Arguments = args[4];
                                }
                                Process.Start(processStartInfo);
                            }
                        }
                        catch (Win32Exception exception)
                        {
                            if (exception.NativeErrorCode != 1223)
                                throw;
                        }
                        Application.Exit();
                    }
                };
                _backgroundWorker.RunWorkerAsync();
            }
            labelInformation.Text = @"参数不正确...";
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _backgroundWorker?.CancelAsync();
        }
    }
}
