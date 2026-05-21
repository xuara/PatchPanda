using System.Text.Json;

namespace PatchPanda.Web.Services.Background;

internal class UpdateBackgroundService(
    IServiceScopeFactory serviceProvider,
    JobRegistry jobRegistry,
    JobQueue queue
) : IHostedService, IDisposable
{
    private const int JobTimeoutSeconds = Limits.UpdateJobTimeoutSeconds;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;

    public void Dispose()
    {
        _cts?.Cancel();
        _processingTask?.GetAwaiter().GetResult();
        _cts?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UpdateBackgroundService>>();

        logger.LogInformation("Update background service starting (queue consumer)");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = Task.Run(() => ProcessQueueAsync(_cts.Token), _cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<UpdateBackgroundService>>();

        logger?.LogInformation("Update background service stopping");

        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        try
        {
            if (_processingTask is not null)
            {
                await _processingTask.WaitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessJob<TJob>(
        TJob job,
        ILogger<UpdateBackgroundService> logger,
        Func<string, CancellationToken, Task> function,
        CancellationToken cancellationToken
    )
        where TJob : AbstractJob
    {
        if (!jobRegistry.TryStartProcessing(job.Sequence))
            return;

        var jobName = job.GetType().Name;

        jobRegistry.AppendOutput(job.Sequence, $"Starting job type {jobName} (queued)...");

        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(1, JobTimeoutSeconds))
        );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        try
        {
            await function.Invoke(jobName, linkedCts.Token);

            jobRegistry.AppendOutput(job.Sequence, $"{jobName} finished.");
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Job {JobName} timed out after {TimeoutSeconds} seconds",
                jobName,
                JobTimeoutSeconds
            );
            jobRegistry.AppendOutput(
                job.Sequence,
                $"{jobName} timed out after {JobTimeoutSeconds} seconds."
            );
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Job {JobName} cancelled due to host shutdown", jobName);
            jobRegistry.AppendOutput(job.Sequence, $"{jobName} cancelled due to host shutdown.");
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Job {JobName} timed out", jobName);
            jobRegistry.AppendOutput(job.Sequence, $"{jobName} timed out: " + ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Error while running job {JobName}, full job: {Job}",
                jobName,
                JsonSerializer.Serialize(job)
            );
            jobRegistry.AppendOutput(job.Sequence, $"{jobName} failed: " + ex.Message);
        }
        finally
        {
            jobRegistry.FinishProcessing(job.Sequence);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var job in queue.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                using var scope = serviceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<
                    ILogger<UpdateBackgroundService>
                >();

                switch (job)
                {
                    case UpdateJob updateJob:
                        await ProcessJob(
                            updateJob,
                            logger,
                            async (jobName, jobCancellationToken) =>
                            {
                                var updateService =
                                    scope.ServiceProvider.GetRequiredService<UpdateService>();
                                var dbFactory = scope.ServiceProvider.GetRequiredService<
                                    IDbContextFactory<DataContext>
                                >();

                                await using var db = await dbFactory.CreateDbContextAsync(
                                    jobCancellationToken
                                );
                                var app = await db
                                    .Containers.Include(x => x.NewerVersions)
                                    .FirstOrDefaultAsync(
                                        x => x.Id == updateJob.ContainerId,
                                        jobCancellationToken
                                    );

                                if (app is null)
                                {
                                    logger.LogInformation(
                                        "Disregarding job for missing container {ContainerId}",
                                        updateJob.ContainerId
                                    );
                                    jobRegistry.AppendOutput(
                                        updateJob.Sequence,
                                        "Container not found."
                                    );
                                    return;
                                }

                                var targetVersion = app.NewerVersions.FirstOrDefault(v =>
                                    v.Id == updateJob.TargetVersionId
                                );

                                if (targetVersion is null)
                                    return;

                                await updateService.Update(
                                    app,
                                    false,
                                    targetVersion,
                                    (line) => jobRegistry.AppendOutput(updateJob.Sequence, line),
                                    updateJob.IsAutomatic,
                                    jobCancellationToken
                                );
                            },
                            cancellationToken
                        );
                        break;

                    case ResetAllJob resetAllJob:
                        await ProcessJob(
                            resetAllJob,
                            logger,
                            async (jobName, jobCancellationToken) =>
                            {
                                var dockerService =
                                    scope.ServiceProvider.GetRequiredService<DockerService>();
                                await dockerService.ResetComposeStacks(jobCancellationToken);
                            },
                            cancellationToken
                        );
                        break;

                    case CheckForUpdatesAllJob checkForUpdatesAllJob:
                        await ProcessJob(
                            checkForUpdatesAllJob,
                            logger,
                            async (jobName, jobCancellationToken) =>
                            {
                                var updateService =
                                    scope.ServiceProvider.GetRequiredService<UpdateService>();
                                await updateService.CheckAllForUpdates(jobCancellationToken);
                            },
                            cancellationToken
                        );
                        break;

                    case RestartStackJob restartStackJob:
                        await ProcessJob(
                            restartStackJob,
                            logger,
                            async (jobName, jobCancellationToken) =>
                            {
                                var dockerService =
                                    scope.ServiceProvider.GetRequiredService<DockerService>();
                                var dbFactory = scope.ServiceProvider.GetRequiredService<
                                    IDbContextFactory<DataContext>
                                >();

                                await using var db = await dbFactory.CreateDbContextAsync(
                                    jobCancellationToken
                                );
                                var stack = await db
                                    .Stacks.Include(x => x.Apps)
                                    .FirstOrDefaultAsync(
                                        x => x.Id == restartStackJob.StackId,
                                        jobCancellationToken
                                    );

                                if (stack is null)
                                {
                                    logger.LogInformation(
                                        "Disregarding job for missing stack {StackId}",
                                        restartStackJob.StackId
                                    );
                                    jobRegistry.AppendOutput(
                                        restartStackJob.Sequence,
                                        "Stack not found."
                                    );
                                    return;
                                }

                                await dockerService.RunDockerComposeOnPath(
                                    stack,
                                    "restart",
                                    (line) =>
                                        jobRegistry.AppendOutput(restartStackJob.Sequence, line),
                                    jobCancellationToken
                                );
                            },
                            cancellationToken
                        );
                        break;

                    default:
                        logger.LogError(
                            "Disregarding unknown job type {JobType}",
                            job.GetType().Name
                        );
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}
