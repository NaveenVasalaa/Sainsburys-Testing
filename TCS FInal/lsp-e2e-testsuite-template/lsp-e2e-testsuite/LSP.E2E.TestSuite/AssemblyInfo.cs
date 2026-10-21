using LSP.E2E.TestSuite;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = true)]
[assembly: CaptureConsole]
[assembly: TestPipelineStartup(typeof(Pipeline))]