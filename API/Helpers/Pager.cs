using System.Runtime.Serialization;

namespace API.Helpers
{
    [DataContract]
    public class Pager<T> where T : class
    {
        [DataMember(Order = 1)]
        public int PageIndex { get; private set; }
        [DataMember(Order = 2)]
        public int PageSize { get; private set; }
        [DataMember(Order = 3)]
        public int Total { get; private set; }
        [DataMember(Order = 4)]
        public string Search { get; private set; }
        [DataMember(Order = 5)]
        public IEnumerable<T> Registers { get; private set; }

            public Pager(IEnumerable<T> registers, int total, int pageIndex, int pageSize, string search)
            {
                Registers = registers;
                Total = total;
                PageIndex = pageIndex;
                PageSize = pageSize;
                Search = search;
        }
        [DataMember(Order = 6)]
        public int TotalPages
            {
            get
            {
                return (int)Math.Ceiling((double)Total / PageSize);
            }
            private set { }
               
            }
        [IgnoreDataMember]
        public bool HasPreviousPage
            {
                get { return PageIndex > 1; }
            }
        [IgnoreDataMember]
        public bool HasNextPage
            {
                get { return (PageIndex < TotalPages); }
            }

        }

       
}
