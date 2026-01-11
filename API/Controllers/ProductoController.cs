using API.DTO;
using API.Helpers;
using Asp.Versioning;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiVersion(0.8)]
    [ApiVersion(1)]
    [Authorize(Roles = "Administrador")]
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
        public async Task<ActionResult<Pager<ProductoListDTO>>> Get([FromQuery] Params productParams)
        {
            var resultado = await _unitOfWork.Productos.GetAllAsync(productParams.PageIndex, productParams.PageSize, productParams.Search);

            var listaProductosDTO = _mapper.Map<List<ProductoListDTO>>(resultado.registros);
            return Ok(new Pager<ProductoListDTO>(listaProductosDTO, resultado.totalRegistros, productParams.PageIndex, productParams.PageSize, productParams.Search));
        }


        //[HttpGet]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<IEnumerable<ProductoListDTO>>> GetAntiguo()
        //{
        //    var productos = await _unitOfWork.Productos.GetAllAsync();
        //    return Ok(_mapper.Map<List<ProductoListDTO>>(productos));
        //}

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
        public async Task<ActionResult> Post([FromBody] ProductoAddUpdateDTO productoDTO)
        {
            var producto = _mapper.Map<Producto>(productoDTO);
            _unitOfWork.Productos.Add(producto);
            await _unitOfWork.SaveAsync();

            if (producto == null)
                return BadRequest();

            productoDTO.Id = producto.Id;
            return CreatedAtAction(nameof(Get), new { id = productoDTO.Id }, productoDTO);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Put([FromBody] ProductoAddUpdateDTO productoDTO)
        {
            if (productoDTO == null)
                return NotFound();

            var producto = _mapper.Map<Producto>(productoDTO);

            _unitOfWork.Productos.Update(producto);
            await _unitOfWork.SaveAsync();

            return Ok(productoDTO);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]       
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public async Task<ActionResult> Delete(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);

            if (producto == null)
                return NotFound();

            _unitOfWork.Productos.Remove(producto);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }

    }
}
