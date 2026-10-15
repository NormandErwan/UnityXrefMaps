using System.Threading.Tasks;

namespace UnityXrefMaps.Tests;

public class UtilsTests
{
    [Fact]
    public async Task RunCommand_SuccessfulProcess_ReturnsZero()
    {
        // `true` always exits with code 0.
        // RunCommand must surface the exit code so callers can detect failures.
        int exitCode = await Utils.RunCommand("true", "", _ => { }, _ => { });
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunCommand_FailingProcess_ReturnsNonZero()
    {
        // `false` always exits with code 1.
        // Before the fix, RunCommand returned void so exit codes were silently discarded.
        int exitCode = await Utils.RunCommand("false", "", _ => { }, _ => { });
        Assert.Equal(1, exitCode);
    }
}
