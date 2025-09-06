using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class PedidosController : BaseController
    {
        private readonly SARVADbContext _db;

        public PedidosController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        public async Task<IActionResult> LerDadosPedido(DateTime? dataInicio, DateTime? dataFim, string filtroEmpresa)
        {
            var idUsuario = Convert.ToString(UsuarioIdLogado);

            var query = _db.Pedidos
                           .Include(p => p.Empresa)
                           .Where(x => x.id_usuario == idUsuario);

            if (dataInicio.HasValue && dataFim.HasValue)
            {
                query = query.Where(x => x.data_pedido >= dataInicio.Value && x.data_pedido <= dataFim.Value);
            }
            if (!string.IsNullOrEmpty(filtroEmpresa))
            {
                query = query.Where(x => x.Empresa.razao_social == filtroEmpresa);
            }

            var listOfData = await query.OrderBy(x => x.id).ToListAsync();
            return View(listOfData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarPedido(Empresa model)
        {
            var idUsuario = Convert.ToString(UsuarioIdLogado);
            var empresa = await _db.Empresas.FirstOrDefaultAsync(x => x.razao_social == model.razao_social);
            if (empresa == null) return NotFound("Empresa não encontrada.");

            DateTime now = DateTime.Now;
            Pedido pedido = new Pedido
            {
                id_usuario = idUsuario,
                id_empresa = empresa.id,
                data_pedido = now,
                data_vencimento = now.AddDays(21)
            };

            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();

            return RedirectToAction("CatalogoItemVenda", "Item_Venda", new { idPedido = pedido.id, idEmpresa = pedido.id_empresa });
        }

        [HttpGet]
        public async Task<IActionResult> EditarPedido(int id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(x => x.id == id && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (pedido == null) return NotFound();
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPedido(Pedido model)
        {
            if (!ModelState.IsValid) return View(model);

            var pedido = await _db.Pedidos.FirstOrDefaultAsync(x => x.id == model.id && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (pedido == null) return NotFound();

            // Atualizar propriedades
            pedido.id_empresa = model.id_empresa;
            pedido.valor = model.valor;
            pedido.data_pedido = model.data_pedido;
            pedido.data_vencimento = model.data_vencimento;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(LerDadosPedido));
        }

        [HttpGet]
        public async Task<IActionResult> FinalizarPedido(int idPedido)
        {
            var pedido = await _db.Pedidos
                                  .Include(p => p.Item_Pedido)
                                  .ThenInclude(ip => ip.Item_Venda)
                                  .ThenInclude(iv => iv.Produto)
                                  .ThenInclude(p => p.Ciclo)
                                  .Include(p => p.Item_Pedido)
                                  .ThenInclude(ip => ip.Item_Venda)
                                  .ThenInclude(iv => iv.Venda)
                                  .ThenInclude(v => v.Cliente)
                                  .FirstOrDefaultAsync(x => x.id == idPedido && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (pedido == null) return NotFound();

            var viewModel = new PedidoViewModel
            {
                Pedido = pedido,
                ItensDetalhados = pedido.Item_Pedido.Select(ip => new ItemPedidoDetalhe
                {
                    VendaId = ip.id_venda_IV,
                    NomeCliente = ip.Item_Venda.Venda.Cliente.nome,
                    NomeProduto = ip.Item_Venda.Produto.nome,
                    NomeCiclo = ip.Item_Venda.Produto.Ciclo.nome,
                    ValorCalculado = ip.valor * ip.Item_Venda.quantidade,
                    Quantidade = ip.Item_Venda.quantidade
                }).ToList(),
                ProdutosAgrupados = pedido.Item_Pedido
                                          .GroupBy(ip => ip.Item_Venda.Produto.nome)
                                          .Select(g => new ProdutoAgrupado
                                          {
                                            NomeProduto = g.Key,
                                            QuantidadeTotal = g.Sum(x => x.Item_Venda.quantidade)
                                          }).ToList()
                };

            viewModel.ValorTotal = viewModel.ItensDetalhados.Sum(i => i.ValorCalculado);
            viewModel.ValorAPagar = 0.7M * viewModel.ValorTotal;

            return View(viewModel);

            //ViewBag.Pedido = pedido;
            //return View(pedido.Item_Pedido.OrderBy(ip => ip.id_venda_IV).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarPedidoPost(int idPedido, decimal valorAPagar)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(x => x.id == idPedido && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (pedido == null) return NotFound();

            pedido.valor = valorAPagar;
            await _db.SaveChangesAsync();
            return RedirectToAction("LerDadosPedido", "Pedidos");
        }

        public async Task<IActionResult> DetalhesPedido(int id)
        {
            // 1. UMA ÚNICA CONSULTA EFICIENTE AO BANCO
            // Usamos .Include() e .ThenInclude() para carregar tudo de uma vez, evitando N+1 queries.
            var pedido = await _db.Pedidos
                .Include(p => p.Item_Pedido)
                    .ThenInclude(ip => ip.Item_Venda)
                    .ThenInclude(iv => iv.Produto)
                .FirstOrDefaultAsync(p => p.id == id && p.id_usuario == Convert.ToString(UsuarioIdLogado));

            if (pedido == null)
            {
                return NotFound();
            }

            // 2. LÓGICA DE AGRUPAMENTO (MOVIDA DA VIEW PARA CÁ)
            // O GroupBy é muito mais eficiente para agrupar e somar os produtos.
            var produtosAgrupados = pedido.Item_Pedido
                .GroupBy(ip => ip.Item_Venda.Produto.nome)
                .Select(g => new ProdutoAgrupado
                {
                    NomeProduto = g.Key,
                    QuantidadeTotal = g.Sum(x => x.Item_Venda.quantidade)
                })
                .OrderBy(p => p.NomeProduto) // Opcional: ordenar por nome
                .ToList();

            // 3. MONTAR O VIEWMODEL
            // Criamos o objeto que levará TODOS os dados para a View, sem usar ViewBag.
            var viewModel = new DetalhesPedidoViewModel
            {
                Pedido = pedido,
                ProdutosAgrupados = produtosAgrupados
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverPedido(int id)
        {
            var pedido = await _db.Pedidos
                                .Include(p => p.Item_Pedido)
                                .FirstOrDefaultAsync(x => x.id == id && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (pedido == null) return NotFound();

            _db.Item_Pedido.RemoveRange(pedido.Item_Pedido); // Remove itens primeiro
            _db.Pedidos.Remove(pedido);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(LerDadosPedido));
        }
    }
}