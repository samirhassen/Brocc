namespace nCustomer.DbModel
{
    public class CustomerCheckpointCode
    {
        public int CustomerCheckpointId { get; set; }
        public string Code { get; set; }
        public CustomerCheckpoint Checkpoint { get; set; }
    }
}