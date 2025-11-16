using API.DTO;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Infrastruture.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class ProductoController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductoController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Producto>>> Get()
        {
            var productos = await _unitOfWork.Productos.GetAllAsync();
            return Ok(productos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductoDTO>> Get(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
            return Ok(_mapper.Map<ProductoDTO>(producto));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Post([FromBody] Producto producto)
        {

            _unitOfWork.Productos.Add(producto);
            _unitOfWork.Save();
            if (producto == null)
                return BadRequest();


            return CreatedAtAction(nameof(Post), new { id = producto.Id }, producto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Put([FromBody] Producto producto)
        {
            _unitOfWork.Productos.Update(producto);
            _unitOfWork.Save();
            return Ok(producto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]       
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public async Task<ActionResult> Delete(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);

            if (producto == null)
                return NotFound();

            _unitOfWork.Productos.Remove(producto);
            _unitOfWork.Save();
            return Ok();
        }

    }
}
