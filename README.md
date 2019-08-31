Project was migrated to [SegmentDownload](https://github.com/golavr/SegmentDownload)

# SegmentDownloader

[![Nuget downloads](https://img.shields.io/nuget/v/SegmentDownloader.Core.svg?label=SegmentDownloader.Core&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SegmentDownloader.Core/)
[![Nuget downloads](https://img.shields.io/nuget/v/SegmentDownloader.Protocol.svg?label=SegmentDownloader.Protocol&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SegmentDownloader.Protocol/)

![MyDwnloader1](src/docs/MyDwnloader1.png)

## Changes
- Maintain nuget packages
  - [SegmentDownloader.Core](https://www.nuget.org/packages/SegmentDownloader.Core)
  - [SegmentDownloader.Protocol](https://www.nuget.org/packages/SegmentDownloader.Protocol)
- Fix large file size issues.
- Decouple SegmentDownloader.Core from System.Windows.Forms and System.Drawing dependencies - more lightweight and less depended packages.
- Fix network failure during segments download - override default value of 5 minutes and set stream timeout value to 30 sec.
- Set EndedWithError state when reaching max retries.
- Add InitialRetryDelay and InitialMaxRetries settings - differentiate first info fetch from downloading phase which gives you error faster in case max retry/delay have high numbers.
- Add [sample](https://github.com/golavr/SegmentDownloader/tree/master/src/SegmentDownloader.Sample) console project.
