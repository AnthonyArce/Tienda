namespace API.DTO
{
    public class ProductoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public DateTime FechaCreacion { get; set; }
        public MarcaDTO Marca { get; set; }
        public CategoriaDTO Categoria { get; set; }
    }
}
