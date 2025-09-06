using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class ProdutosController : BaseController
    {
        private readonly SARVADbContext _db;

        public ProdutosController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        public async Task<IActionResult> CatalogoProdutos(int idVenda, int idEmpresa, int? filtroCodigo, string filtroProduto, string filtroCiclo, string razaoSocial)
        {
            ViewBag.IdVenda = idVenda;
            ViewBag.IdEmpresa = idEmpresa;

            var listIV = await _db.Item_Venda.Where(x => x.id_venda == idVenda).ToListAsync();
            ViewBag.CodigosIV = listIV.Select(item => item.codigo_produto).ToList();
            ViewBag.IdCicloIV = listIV.Select(item => item.id_ciclo_produto).ToList();

            var query = _db.Produtos
                           .Include(p => p.Ciclo)
                           .Include(p => p.Empresa)
                           .AsQueryable();

            if (filtroCodigo.HasValue)
            {
                query = query.Where(x => x.codigo == filtroCodigo.Value);
            }
            else if (!string.IsNullOrEmpty(filtroProduto))
            {
                query = query.Where(x => x.nome.Contains(filtroProduto));
            }
            else if (!string.IsNullOrEmpty(filtroCiclo))
            {
                query = query.Where(x => x.Ciclo.nome.Contains(filtroCiclo));
            }
            else if (!string.IsNullOrEmpty(razaoSocial))
            {
                query = query.Where(x => x.Empresa.razao_social.Contains(razaoSocial));
            }

            var listOfData = await query.OrderBy(x => x.nome).ToListAsync();
            return View(listOfData);
        }

        [HttpGet]
        public IActionResult AdicionarProduto(int idEmpresa)
        {
            ViewBag.IdEmpresa = idEmpresa;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarProduto(Produto model, string cicloNome)
        {
            var ciclo = await _db.Ciclos.FirstOrDefaultAsync(x => x.nome == cicloNome);
            if (ciclo == null)
            {
                ModelState.AddModelError("", "Esse ciclo não existe");
                return View(model);
            }

            if (await _db.Produtos.AnyAsync(x => x.codigo == model.codigo && x.id_ciclo == ciclo.id))
            {
                ViewBag.JaExiste = "Esse produto já foi requisitado neste ciclo";
                return View(model);
            }

            if (ModelState.IsValid)
            {
                if (User.IsInRole("Admin"))
                {
                    model.flag = true;
                }
                else if (User.IsInRole("Vendedor"))
                {
                    model.flag = false;
                }
                model.id_usuario = Convert.ToString(UsuarioIdLogado);
                model.id_ciclo = ciclo.id;
                _db.Produtos.Add(model);
                await _db.SaveChangesAsync();
                return RedirectToAction("CatalogoProdutos", "Produtos", new { idVenda = -1, idEmpresa = -1 });
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarProdutoRequisitado(Empresa model)
        {
            var empresa = await _db.Empresas.FirstOrDefaultAsync(x => x.razao_social == model.razao_social);
            if (empresa == null) return NotFound("Empresa não encontrada");

            return RedirectToAction("AdicionarProduto", "Produtos", new { idEmpresa = empresa.id });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditarProduto(int codigo)
        {
            var data = await _db.Produtos.Include(p => p.Ciclo).FirstOrDefaultAsync(x => x.codigo == codigo);
            if (data == null) return NotFound();

            ViewBag.Codigo = codigo;
            ViewBag.Nome = data.nome;
            ViewBag.Ciclo = data.Ciclo.nome;
            return View(data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProduto(Produto model, string cicloNome)
        {
            var data = await _db.Produtos.FirstOrDefaultAsync(x => x.codigo == model.codigo);
            if (data == null) return NotFound();

            var ciclo = await _db.Ciclos.FirstOrDefaultAsync(x => x.nome == cicloNome);
            if (ciclo == null)
            {
                ModelState.AddModelError("", "Ciclo não encontrado");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                data.nome = model.nome;
                data.valor = model.valor;
                data.id_ciclo = ciclo.id;
                data.pontos = model.pontos;
                data.flag = model.flag;
                // id_usuario e id_empresa geralmente não são alterados em uma edição
                await _db.SaveChangesAsync();
                return RedirectToAction("CatalogoProdutos", "Produtos", new { idVenda = -1, idEmpresa = -1 });
            }
            return View(model);
        }

        public async Task<IActionResult> DetalhesProduto(int codigo)
        {
            var data = await _db.Produtos
                .Include(p => p.Empresa)
                .Include(p => p.Ciclo)
                .FirstOrDefaultAsync(x => x.codigo == codigo);
            if (data == null) return NotFound();
            ViewBag.Code = codigo;
            ViewBag.Ciclo = data.Ciclo.nome;
            return View(data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarProduto(int codigo, int idCiclo)
        {
            var produtoParaDeletar = await _db.Produtos
                .Include(p => p.Item_Venda)
                .FirstOrDefaultAsync(x => x.codigo == codigo && x.id_ciclo == idCiclo);

            if (produtoParaDeletar == null)
            {
                return NotFound();
            }

            if (produtoParaDeletar.Item_Venda.Any())
            {
                TempData["ErrorMessage"] = $"Não é possível excluir o produto '{produtoParaDeletar.nome}', pois ele já está registrado em uma ou mais vendas.";
            }
            else
            {
                _db.Produtos.Remove(produtoParaDeletar);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Produto '{produtoParaDeletar.nome}' foi excluído com sucesso.";
            }

            return RedirectToAction("CatalogoProdutos", "Produtos", new { idVenda = -1, idEmpresa = -1 });
        }
    }
}