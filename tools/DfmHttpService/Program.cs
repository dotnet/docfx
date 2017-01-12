// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;

    public class Program
    {
        public static void Main(string[] args)
        {
            var handler = new CompositeHandler(
                new List<IHttpHandler>
                {
                    new DfmPreviewHandler(),
                    new DfmTokenTreeHandler(),
                    new ExitHandler()
                });

            var service = new DfmHttpServer(handler);
            service.Start();
            service.WaitForExit();
        }
    }
}
