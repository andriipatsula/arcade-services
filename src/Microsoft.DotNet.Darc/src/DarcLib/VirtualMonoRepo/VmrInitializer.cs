// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.DotNet.Darc.Models.VirtualMonoRepo;
using Microsoft.DotNet.DarcLib.Helpers;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Microsoft.DotNet.DarcLib.VirtualMonoRepo;

public class VmrInitializer : VmrManagerBase, IVmrInitializer
{
    // Message shown when initializing an individual repo for the first time
    private const string InitializationCommitMessage =
        """
        [{name}] Initial pull of the individual repository ({newShaShort})

        Original commit: {remote}/commit/{newSha}
        """;

    private readonly ILogger<VmrUpdater> _logger;

    public VmrInitializer(
        IProcessManager processManager,
        IRemoteFactory remoteFactory,
        ILogger<VmrUpdater> logger,
        IVmrManagerConfiguration configuration,
        IReadOnlyCollection<SourceMapping> mappings)
        : base(processManager, remoteFactory, logger, mappings, configuration.VmrPath, configuration.TmpPath)
    {
        _logger = logger;
    }

    public async Task InitializeVmr(SourceMapping mapping, string? targetRevision, CancellationToken cancellationToken)
    {
        if (File.Exists(Path.Combine(SourcesPath, $".{mapping.Name}")))
        {
            throw new EmptySyncException($"Repository {mapping.Name} already exists");
        }

        _logger.LogInformation("Initializing {name} at {revision}..", mapping.Name, targetRevision ?? mapping.DefaultRef);

        string clonePath = await CloneOrPull(mapping);
        cancellationToken.ThrowIfCancellationRequested();

        using var clone = new Repository(clonePath);
        var commit = GetCommit(clone, (targetRevision is null || targetRevision == HEAD) ? null : targetRevision);

        string patchPath = GetPatchFilePath(mapping);
        await CreatePatch(mapping, clonePath, Constants.EmptyGitObject, commit.Id.Sha, patchPath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await ApplyPatch(mapping, patchPath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await TagRepo(mapping, commit.Id.Sha);
        cancellationToken.ThrowIfCancellationRequested();

        await ApplyVmrPatches(mapping, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Commit but do not add files (they were added to index directly)
        var message = PrepareCommitMessage(InitializationCommitMessage, mapping, newSha: commit.Id.Sha);
        Commit(message, DotnetBotCommitSignature);

        _logger.LogInformation("Initialization of {name} finished", mapping.Name);
    }
}