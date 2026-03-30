namespace Santander.DevCodingTest.Tests.Unit;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    private readonly bool _simulateTimeout;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
        _simulateTimeout = false;
    }

    public MockHttpMessageHandler(bool simulateTimeout)
    {
        _handler = _ => new HttpResponseMessage();
        _simulateTimeout = simulateTimeout;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_simulateTimeout)
            throw new TaskCanceledException("Simulated timeout", new TimeoutException());

        return Task.FromResult(_handler(request));
    }
}