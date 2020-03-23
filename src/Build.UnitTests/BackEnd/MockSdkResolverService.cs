﻿using Microsoft.Build.BackEnd;
using Microsoft.Build.BackEnd.Logging;
using Microsoft.Build.BackEnd.SdkResolution;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using System;

namespace Microsoft.Build.Engine.UnitTests.BackEnd
{
    internal class MockSdkResolverService : IBuildComponent, ISdkResolverService
    {
        public Action<INodePacket> SendPacket { get; }

        public void ClearCache(int submissionId)
        {
        }

        public void ClearCaches()
        {
        }

        public Build.BackEnd.SdkResolution.SdkResult ResolveSdk(int submissionId, SdkReference sdk, LoggingContext loggingContext, IElementLocation sdkReferenceLocation, string solutionPath, string projectPath, bool interactive)
        {
            return null;
        }

        public void InitializeComponent(IBuildComponentHost host)
        {
        }

        public void ShutdownComponent()
        {
        }
    }
}
