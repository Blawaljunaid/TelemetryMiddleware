using EventStore.ClientAPI;

namespace Fingrid.Monitoring
{
    public class EventStoreConnector
    {
        private static IEventStoreConnection connection;

        public static void Connect(string connectionString)
        {
            connection = EventStoreConnection.Create(connectionString);
            connection.ConnectAsync().Wait();
        }

        public static IEventStoreConnection GetConnection()
        {
            return connection;
        }
    }
}