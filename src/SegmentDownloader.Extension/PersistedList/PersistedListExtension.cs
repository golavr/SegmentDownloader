using System;
using System.Threading;
using System.Collections.Generic;
using SegmentDownloader.Core;
using System.IO;
using System.Xml.Serialization;
using SegmentDownloader.Core.Instrumentation;
using System.Diagnostics;
using System.Linq;
using SegmentDownloader.Common.UI.Extensions;

namespace SegmentDownloader.Extension.PersistedList
{
    public class PersistedListExtension : IExtension, IDisposable
    {
        [Serializable]
        public class DownloadItem
        {
            public ResourceLocation rl;

            public ResourceLocation[] mirrors;

            [XmlAttribute("lf")]
            public string LocalFile;

            public RemoteFileInfo remoteInfo;

            [XmlAttribute("segCnt")]
            public int requestedSegments;

            [XmlAttribute("dt")]
            public DateTime createdDateTime;

            public SegmentItem[] Segments;

            public SerializableDictionary<string, object> extendedProperties;
        }

        [Serializable]
        public class SegmentItem
        {
            [XmlAttribute("i")]
            public int Index;

            [XmlAttribute("isp")]
            public long InitialStartPositon;

            [XmlAttribute("sp")]
            public long StartPositon;

            [XmlAttribute("ep")]
            public long EndPosition;
        }

        private const int SaveListIntervalInSeconds = 120;

        private readonly XmlSerializer serializer;
        private Timer timer;

        private readonly object SaveFromDispose = new object();

        #region IExtension Members

        public string Name => "Persisted Download List";

        public IUIExtension UIExtension => null;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            DownloadManager.Instance.PauseAll();

            PersistList(SaveFromDispose);
        }

        #endregion

        #region Methods

        private void PersistList(object state)
        {
            var downloadsToSave = new List<DownloadItem>();

            using (DownloadManager.Instance.LockDownloadList(false))
            {
                IList<Downloader> downloads = DownloadManager.Instance.Downloads;

                foreach (var download in downloads)
                {
                    if (download.State == DownloaderState.Ended)
                    {
                        continue;
                    }

                    DownloadItem di = new DownloadItem
                    {
                        LocalFile = download.LocalFile,
                        rl = download.ResourceLocation,
                        mirrors = download.Mirrors.ToArray(),
                        remoteInfo = download.RemoteFileInfo,
                        requestedSegments = download.RequestedSegments,
                        createdDateTime = download.CreatedDateTime,
                        extendedProperties = new SerializableDictionary<string, object>(download.ExtendedProperties)
                    };

                    using (download.LockSegments())
                    {
                        di.Segments = new SegmentItem[download.Segments.Count];

                        for (int j = 0; j < download.Segments.Count; j++)
                        {
                            SegmentItem si = new SegmentItem();
                            Segment seg = download.Segments[j];

                            si.Index = seg.Index;
                            si.InitialStartPositon = seg.InitialStartPosition;
                            si.StartPositon = seg.StartPosition;
                            si.EndPosition = seg.EndPosition;

                            di.Segments[j] = si;
                        }
                    }

                    downloadsToSave.Add(di);
                }
            }

            SaveObjects(downloadsToSave);
        }

        private string GetDatabaseFile()
        {
            string file = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\" + "downloads.xml";
            return file;
        }

        private void LoadSavedList()
        {
            if (!File.Exists(GetDatabaseFile())) return;
            try
            {
                using (FileStream fs = new FileStream(GetDatabaseFile(), FileMode.Open))
                {
                    DownloadItem[] downloads = (DownloadItem[])serializer.Deserialize(fs);

                    LoadPersistedObjects(downloads);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void SaveObjects(List<DownloadItem> downloadsToSave)
        {
            using (new MyStopwatch("Saving download list"))
            {
                try
                {
                    using (FileStream fs = new FileStream(GetDatabaseFile(), FileMode.Create))
                    {
                        serializer.Serialize(fs, downloadsToSave.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        private static void LoadPersistedObjects(DownloadItem[] downloads)
        {
            foreach (var download in downloads)
            {
                var segments = download.Segments.Select(segment => new Segment
                {
                    Index = segment.Index,
                    InitialStartPosition = segment.InitialStartPositon,
                    StartPosition = segment.StartPositon,
                    EndPosition = segment.EndPosition
                })
                    .ToList();

                var d = DownloadManager.Instance.Add(
                    download.rl,
                    download.mirrors,
                    download.LocalFile,
                    segments,
                    download.remoteInfo,
                    download.requestedSegments,
                    false,
                    download.createdDateTime);

                if (download.extendedProperties == null) continue;
                var propertiesEnumerator = download.extendedProperties.GetEnumerator();

                while (propertiesEnumerator.MoveNext())
                {
                    d.ExtendedProperties.Add(propertiesEnumerator.Current.Key, propertiesEnumerator.Current.Value);
                }
                propertiesEnumerator.Dispose();
            }
        }

        #endregion

        #region Constructor

        public PersistedListExtension()
        {
            serializer = new XmlSerializer(typeof(DownloadItem[]));

            LoadSavedList();

            TimerCallback refreshCallBack = PersistList;
            TimeSpan refreshInterval = TimeSpan.FromSeconds(SaveListIntervalInSeconds);
            timer = new Timer(refreshCallBack, null, new TimeSpan(-1), refreshInterval);
        }

        #endregion
    }
}