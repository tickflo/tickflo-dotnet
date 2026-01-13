using Tickflo.Core.Services.Storage;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class RustFSStorageServiceTests
{
    [Fact]
    public void Placeholder_Class_Exists()
    {
        var placeholder = new RustFSStorageServicePlaceholder();
        Assert.NotNull(placeholder);
    }
}
