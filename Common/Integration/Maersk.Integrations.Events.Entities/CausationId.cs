using System.Collections.Generic;

namespace Maersk.Integrations.Events.Entities
{
    public class CausationId
    {
        public DataObject DataObject { get; private set; }

        public List<string> Ids { get; private set; }

        private CausationId()
        {
            Ids = new List<string>();
        }

        public CausationId(DataObject dataObject, List<string> ids): this()
        {
            DataObject = dataObject;
            Ids = ids;
        }
    }
}