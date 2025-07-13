using Newtonsoft.Json;

namespace EventBus.Base.Events;

public class IntegrationEvent
{
    [JsonProperty]
    public Guid Id { get; set; }

    [JsonProperty]
    public DateTime CreatedDate { get; set; }
    public IntegrationEvent(Guid ıd, DateTime createdDate)
    {
        Id = ıd;
        CreatedDate = createdDate;
    }
    [JsonConstructor]
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.Now;
    }
}
