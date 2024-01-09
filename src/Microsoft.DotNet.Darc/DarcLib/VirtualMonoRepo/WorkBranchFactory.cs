﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Microsoft.DotNet.DarcLib.VirtualMonoRepo;

public interface IWorkBranchFactory
{
    Task<IWorkBranch> CreateWorkBranchAsync(ILocalGitRepo repo, string branchName);
}

public class WorkBranchFactory(ILogger<WorkBranch> logger) : IWorkBranchFactory
{
    private readonly ILogger<WorkBranch> _logger = logger;

    public async Task<IWorkBranch> CreateWorkBranchAsync(ILocalGitRepo repo, string branchName)
    {
        var result = await repo.ExecuteGitCommand("rev-parse", "--abbrev-ref", "HEAD");
        result.ThrowIfFailed("Failed to determine the current branch");

        var originalBranch = result.StandardOutput.Trim();
        if (originalBranch == branchName)
        {
            var message = $"You are already on branch {branchName}. " +
                            "Previous sync probably failed and left the branch unmerged. " +
                            "To complete the sync checkout the original branch and try again.";

            throw new Exception(message);
        }

        _logger.LogInformation("Creating a temporary work branch {branchName}", branchName);

        await repo.CreateBranchAsync(branchName, overwriteExistingBranch: true);

        return new WorkBranch(repo, _logger, originalBranch, branchName);
    }
}
