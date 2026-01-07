using System.Runtime.Serialization;

namespace API.DTO
{
    public class ProductoListDTO
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Nombre { get; set; }
        [DataMember]
        public decimal Precio { get; set; }
        [DataMember]
        public int MarcaId { get; set; }
        [DataMember]
        public string Marca { get; set; }
        [DataMember]
        public int CategoriaId { get; set; }
        [DataMember]
        public string Categoria { get; set; }
    }
}
