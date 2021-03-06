﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TechSvr.Utils.Files
{
    public delegate void Completed(string key);

    public class MyFileSystemWather
    {
        private FileSystemWatcher fsWather;

        private Hashtable hstbWather;

        public event RenamedEventHandler OnRenamed;
        public event FileSystemEventHandler OnChanged;
        public event FileSystemEventHandler OnCreated;
        public event FileSystemEventHandler OnDeleted;

        /// <summary> 
        /// 构造函数 
        /// </summary> 
        /// <param name="path">要监控的路径</param> 
        public MyFileSystemWather(string path, string filter)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception("找不到路径：" + path);
            }

            hstbWather = new Hashtable();

            fsWather = new FileSystemWatcher(path);
            // 是否监控子目录
            fsWather.IncludeSubdirectories = false;
            fsWather.Filter = filter;
            fsWather.Renamed += new RenamedEventHandler(fsWather_Renamed);
            fsWather.Changed += new FileSystemEventHandler(fsWather_Changed);
            fsWather.Created += new FileSystemEventHandler(fsWather_Created);
            fsWather.Deleted += new FileSystemEventHandler(fsWather_Deleted);
        }

        /// <summary> 
        /// 开始监控 
        /// </summary> 
        public void Start()
        {
            fsWather.EnableRaisingEvents = true;
        }

        /// <summary> 
        /// 停止监控 
        /// </summary> 
        public void Stop()
        {
            fsWather.EnableRaisingEvents = false;
        }

        /// <summary> 
        /// filesystemWatcher 本身的事件通知处理过程 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        private void fsWather_Renamed(object sender, RenamedEventArgs e)
        {
            lock (hstbWather)
            {
                hstbWather.Add(e.FullPath, e);
            }

        }

   

        private void fsWather_Created(object sender, FileSystemEventArgs e)
        {
            lock (hstbWather)
            {
                hstbWather.Add(e.FullPath, e);
            }
       
        }

     

        private void fsWather_Deleted(object sender, FileSystemEventArgs e)
        {
            lock (hstbWather)
            {
                hstbWather.Add(e.FullPath, e);
            }
      
        }

  
        private void fsWather_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (hstbWather.ContainsKey(e.FullPath))
                {
                    WatcherChangeTypes oldType = ((FileSystemEventArgs)hstbWather[e.FullPath]).ChangeType;
                    if (oldType == WatcherChangeTypes.Created || oldType == WatcherChangeTypes.Changed)
                    {
                        return;
                    }
                }
            }

            lock (hstbWather)
            {
                hstbWather.Add(e.FullPath, e);
            }
        }

        public void WatcherProcess_OnCompleted(string key)
        {
            lock (hstbWather)
            {
                hstbWather.Remove(key);
            }
        }
    }
}
