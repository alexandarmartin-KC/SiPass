using SiPassHealth.SiPass;

namespace SiPassHealth.Web;

public static class MetaEnvelope
{
    public static object Wrap(object payload, SipassConnectionState connection)
    {
        return new
        {
            meta = new
            {
                dataStale = connection.DataStale,
                sipassLastSuccessAt = connection.LastSuccessAt,
                disconnectedSince = connection.DisconnectedSince
            },
            data = payload
        };
    }
}
