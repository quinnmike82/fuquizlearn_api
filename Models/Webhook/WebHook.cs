namespace fuquizlearn_api.Models.Webhook
{
    // type InsertPayload = {
    //     type: 'INSERT'
    //     table: string
    //     schema: string
    //     record: TableRecord<T>
    //     old_record: null
    // }
    // type UpdatePayload = {
    //     type: 'UPDATE'
    //     table: string
    //     schema: string
    //     record: TableRecord<T>
    //     old_record: TableRecord<T>
    // }
    // type DeletePayload = {
    //     type: 'DELETE'
    //     table: string
    //     schema: string
    //     record: null
    //     old_record: TableRecord<T>
    // }
    public class WebhookPayload<T>
    {
        public WebHookType type;
        public string table;
        public string schema;
        public T record;
        public T old_record;
    }

    public enum WebHookType
    {
        INSERT,
        UPDATE,
        DELETE
    }
}