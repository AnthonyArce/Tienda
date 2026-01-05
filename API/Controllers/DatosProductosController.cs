using API.DTO;
using Asp.Versioning;
using AutoMapper;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiVersion(0.8)]
    [ApiVersion(1)]
    //[Route("api/v{version:apiVersion}/[controller]")] //Solo cuando el versionamiento es por URL
    public class DatosProductosController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DatosProductosController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<ProductoListDTO>>> Get()
        {
            var productos = await _unitOfWork.Productos.GetAllAsync();
            return Ok(_mapper.Map<List<ProductoListDTO>>(productos));
        }

        [HttpGet]
        [MapToApiVersion("1")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<ProductoDTO>>> GetProductoDesglasado()
        {
            var productos = await _unitOfWork.Productos.GetAllAsync();
            return Ok(_mapper.Map<List<ProductoDTO>>(productos));
        }
    }
}
