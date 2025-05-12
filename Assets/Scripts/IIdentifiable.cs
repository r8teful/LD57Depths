// Just makes it a bit more structured, also nice to use it in ResourceSystem so we don't have to write the same
// Function over and over again
public interface IIdentifiable {
    ushort ID { get; }
}