using System;
using SelectParser.Queries;
using Utf8Json;

namespace SelectQuery.Evaluation
{
    public class JsonRecordWriter
    {
        private FromClause _from;
        private SelectClause _select;

        public JsonRecordWriter(FromClause from, SelectClause select)
        {
            _from = from;
            _select = select;
        }

        public void Write(ref JsonWriter writer, object obj)
        {
            throw new NotImplementedException();
        }
    }
}