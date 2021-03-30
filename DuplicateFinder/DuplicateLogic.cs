using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.Storage;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Diagnostics;

namespace DuplicateFinder
{
    public class DuplicateLogic : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<byte[], int> uniqueFiles;
        private Dictionary<int, List<int>> matchedDuplicates;

        private int totalFiles;
        private int filesProcessed;

        public DuplicateLogic()
        {
            uniqueFiles = new Dictionary<byte[], int>(new ByteArrayComparer());
            matchedDuplicates = new Dictionary<int, List<int>>();

            Reset();
        }

        private void Reset()
        {
            uniqueFiles.Clear();
            matchedDuplicates.Clear();

            totalFiles = 0;
            filesProcessed = 0;
        }

        public string ProgressPercent
        {
            get
            {
                if (totalFiles != 0)
                    return (int)(((double)filesProcessed / (double)totalFiles) * 100) + "%";
                else
                    return "0%";
            }
        }

        private async void NotifyPropertyChanged(String propertyName = "")
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        //Get files for selected folder and assign tasks
        public async Task AssignTasks(StorageFolder sFolder)
        {
            Reset();

            IReadOnlyList<StorageFile> allFiles = await sFolder.GetFilesAsync();
            List<Task> TaskList = new List<Task>();

            totalFiles = allFiles.Count;

            int numberOfThreads = Environment.ProcessorCount + 1;
            int start = 0;

            for (int i = 0; i < numberOfThreads; i++)
            {
                int startCopy = start;

                TaskList.Add(Task.Run(() => FindDuplicates(allFiles, startCopy, allFiles.Count - 1, numberOfThreads)));
                start++;
            }

            await Task.WhenAll(TaskList);
        }


        private async Task FindDuplicates(IReadOnlyList<StorageFile> allFiles, int start, int end, int increment)
        {
            Stream fileStream;
            HashSet<byte[]> tempHashSet = new HashSet<byte[]>();

            using (MD5 md5 = MD5.Create())
            {
                byte[] hash;
                for (int i = start; i <= end; i += increment)
                {
                    fileStream = await allFiles[i].OpenStreamForReadAsync();
                    hash = md5.ComputeHash(fileStream);

                    lock (uniqueFiles)
                    {
                        if (uniqueFiles.ContainsKey(hash))
                        {
                            lock (matchedDuplicates)
                                matchedDuplicates[uniqueFiles[hash]].Add(i);
                        }
                        else
                        {
                            uniqueFiles.Add(hash, i);
                            matchedDuplicates.Add(i, new List<int>());
                        }
                    }

                    Interlocked.Increment(ref filesProcessed);
                    NotifyPropertyChanged("ProgressPercent");
                }
            }
        }
    }

    //Reference https://stackoverflow.com/questions/1440392/use-byte-as-key-in-dictionary
    public class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public override bool Equals(byte[] first, byte[] second)
        {
            if (first == null || second == null)
                return first == second;

            if (ReferenceEquals(first, second))
                return true;

            if (first.Length != second.Length)
                return false;

            return first.SequenceEqual(second);
        }

        public override int GetHashCode(byte[] obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (obj.Length >= 4)
                return BitConverter.ToInt32(obj, 0);

            // Length occupies at most 2 bits. Might as well store them in the high order byte
            int value = obj.Length;
            foreach (var b in obj)
            {
                value <<= 8;
                value += b;
            }

            return value;
        }
    }
}
