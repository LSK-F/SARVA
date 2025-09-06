// Item_VendaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class Item_VendaController : BaseController
    {
        private readonly SARVADbContext _db;

        public Item_VendaController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarItem_Venda(int codigoIV, int idCiclo, int idEmpresa, int idVenda, bool jaTemIV)
        {
            var itemExistente = await _db.Item_Venda.FirstOrDefaultAsync(x => x.id_venda == idVenda && x.codigo_produto == codigoIV && x.id_ciclo_produto == idCiclo);
            var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.codigo == codigoIV);
            if (produto == null) return NotFound("Produto não encontrado");

            if (itemExistente != null)
            {
                itemExistente.quantidade++;
            }
            else
            {
                var itemVenda = new Item_Venda
                {
                    quantidade = 1,
                    id_venda = idVenda,
                    codigo_produto = codigoIV,
                    id_ciclo_produto = idCiclo,
                    valor = produto.valor
                };
                _db.Item_Venda.Add(itemVenda);
            }

            string action = "";
            string controller = "";
            if (jaTemIV == true)
            {
                action = "FinalizarVenda";
                controller = "Vendas";
            }
            else if (jaTemIV == false)
            {
                action = "CatalogoProdutos";
                controller = "Produtos";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(action, controller, new { idVenda = idVenda });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverItem_Venda(int codigoIV, int idCiclo, int idVenda)
        {
            var itemVenda = await _db.Item_Venda.FirstOrDefaultAsync(x => x.id_venda == idVenda && x.codigo_produto == codigoIV && x.id_ciclo_produto == idCiclo);
            if (itemVenda == null) return NotFound();

            if (itemVenda.quantidade > 1)
            {
                itemVenda.quantidade--;
            }
            else
            {
                _db.Item_Venda.Remove(itemVenda);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("FinalizarVenda", "Vendas", new { idVenda = idVenda });
        }

        public async Task<IActionResult> CatalogoItemVenda(int idPedido, int idEmpresa, int? numVenda, string nomeCliente)
        {
            var idUsuario = Convert.ToString(UsuarioIdLogado);

            // 1. Buscamos a chave composta dos itens que já estão no pedido. Esta parte está correta e já roda em memória.
            var itensNoPedidoAtualKeys = (await _db.Item_Pedido
                .Where(ip => ip.id_pedido == idPedido)
                .ToListAsync())
                .Select(ip => (ip.codigo_IV, ip.id_ciclo_IV, ip.id_venda_IV))
                .ToHashSet();

            // 2. Query base com todos os includes e filtros que podem ser traduzidos para SQL
            var query = _db.Item_Venda
                .Include(iv => iv.Produto).ThenInclude(p => p.Ciclo)
                .Include(iv => iv.Venda).ThenInclude(v => v.Cliente)
                .Where(iv =>
                    iv.Venda.id_usuario == idUsuario &&
                    iv.Venda.id_empresa == idEmpresa &&
                    DateTime.Now >= iv.Produto.Ciclo.dataInicio &&
                    DateTime.Now <= iv.Produto.Ciclo.dataFim
                );

            // Aplicação dos filtros
            if (numVenda.HasValue)
            {
                query = query.Where(iv => iv.id_venda == numVenda.Value);
            }
            if (!string.IsNullOrEmpty(nomeCliente))
            {
                query = query.Where(iv => iv.Venda.Cliente.nome.Contains(nomeCliente));
            }

            // 3. MATERIALIZAÇÃO DA CONSULTA: Trazemos os dados do banco para a memória.
            //    A partir daqui, 'itensDisponiveis' é uma List<Item_Venda>, não mais uma IQueryable.
            var itensDisponiveis = await query.ToListAsync();

            // 4. PROCESSAMENTO EM MEMÓRIA: Agora que os dados estão em uma lista C#,
            //    podemos usar a lógica com a tupla sem problemas.
            var itensParaCatalogo = itensDisponiveis
                .Select(item => new CatalogoItemVendaViewModel
                {
                    ItemVenda = item,
                    // Esta linha agora funciona, pois não está mais sendo traduzida para SQL.
                    EstaNoPedidoAtual = itensNoPedidoAtualKeys.Contains((item.codigo_produto, item.id_ciclo_produto, item.id_venda))
                })
                .ToList(); // Usamos .ToList() aqui, pois a operação já é síncrona.

            // 5. Montamos o ViewModel principal da página
            var pageViewModel = new CatalogoPageViewModel
            {
                ItensDoCatalogo = itensParaCatalogo,
                IdPedido = idPedido,
                IdEmpresa = idEmpresa,
                NumVenda = numVenda,
                NomeCliente = nomeCliente
            };

            return View(pageViewModel);
        }
    }
}