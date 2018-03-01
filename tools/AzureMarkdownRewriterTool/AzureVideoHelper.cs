// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.AzureMarkdownRewriters;
    using Microsoft.DocAsCode.Common;

    public static class AzureVideoHelper
    {
        public static Dictionary<string, AzureVideoInfo> ParseAzureVideoFile(string videoFilePath, bool isMigration)
        {
            if (!File.Exists(videoFilePath))
            {
                Console.WriteLine("Can't find video mapping info file. Skip transform step for video.");
                return null;
            }

            try
            {
                var azureVideoInfoMapping = new Dictionary<string, AzureVideoInfo>();
                if (isMigration)
                {
                    var azureVideoRawInfoMapping = new Dictionary<string, AzureVideoDataItem>();
                    var azureVideoRawInformation = JsonUtility.Deserialize<AzureVideoRawInformation>(videoFilePath);
                    foreach (var videoItem in azureVideoRawInformation.Data)
                    {
                        AzureVideoInfo azureVideoInfo = new AzureVideoInfo();
                        azureVideoInfo.Id = GenerateAzureVideoIdFromAcomUrl(videoItem.AcomUrl);
                        azureVideoInfo.Link = NormalizeVideoLink(videoItem.Channel9PlayerUrl);

                        // If there's already a video with same id we need to judge whether it is necessary to update the information
                        if (azureVideoRawInfoMapping.ContainsKey(azureVideoInfo.Id))
                        {
                            // If submission status of them are same. Update the information based on publish date. Use the latest one. Otherwise, use approved one.
                            if (IsVideoApproved(azureVideoRawInfoMapping[azureVideoInfo.Id].SubmissionStatus) ^ IsVideoApproved(videoItem.SubmissionStatus))
                            {
                                if (IsVideoApproved(azureVideoRawInfoMapping[azureVideoInfo.Id].SubmissionStatus))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (videoItem.Published < azureVideoRawInfoMapping[azureVideoInfo.Id].Published)
                                {
                                    continue;
                                }
                            }
                        }

                        azureVideoRawInfoMapping[azureVideoInfo.Id] = videoItem;
                        azureVideoInfoMapping[azureVideoInfo.Id] = azureVideoInfo;
                    }
                }
                else
                {
                    var azureVideoInfoList = JsonUtility.Deserialize<List<AzureVideoInfo>>(videoFilePath);
                    foreach (var azureVideoInfo in azureVideoInfoList)
                    {
                        azureVideoInfoMapping[azureVideoInfo.Id] = azureVideoInfo;
                    }
                }
                return azureVideoInfoMapping;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Azure vedio json deserialize failed. Skip transform step for video. video file path: {videoFilePath}. Ex: {e}");
                return null;
            }
        }

        private static bool IsVideoApproved(string submissionStatus)
        {
            return submissionStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateAzureVideoIdFromAcomUrl(string acomUrl)
        {
            return acomUrl.Trim().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        private static string NormalizeVideoLink(string videoLink)
        {
            var linkParts = videoLink.Trim().Trim('/').Split('/');

            // Should start with https, otherwise it won't be loaded on the page.
            if (linkParts.First().Equals("http:", StringComparison.OrdinalIgnoreCase))
            {
                linkParts[0] = "https:";
            }

            // Should end with player for ch9.
            if (!linkParts.Last().Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                linkParts = linkParts.Concat(new[] { "player" }).ToArray();
            }

            return string.Join("/", linkParts); ;
        }
    }
}
