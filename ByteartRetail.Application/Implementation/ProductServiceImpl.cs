﻿using System.Collections.Generic;
using Apworks.Repositories;
using Apworks.Specifications;
using Apworks.Storage;
using AutoMapper;
using ByteartRetail.DataObjects;
using ByteartRetail.Domain.Model;
using ByteartRetail.Domain.Repositories;
using ByteartRetail.Domain.Services;
using ByteartRetail.ServiceContracts;
using System;
using System.Linq;

namespace ByteartRetail.Application.Implementation
{
    /// <summary>
    /// 表示与“商品”相关的应用层服务的一种实现。
    /// </summary>
    public class ProductServiceImpl : ApplicationService, IProductService
    {
        #region Private Fields
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategorizationRepository _categorizationRepository;
        private readonly IDomainService _domainService;
        #endregion

        #region Ctor

        /// <summary>
        /// 初始化一个<c>ProductServiceImpl</c>类型的实例。
        /// </summary>
        /// <param name="context">用来初始化<c>ProductServiceImpl</c>类型的仓储上下文实例。</param>
        /// <param name="categoryRepository">类别存储实例</param>
        /// <param name="productRepository">商品存储实例</param>
        /// <param name="categorizationRepository">分类存储实例</param>
        /// <param name="domainService">领域服务实例</param>
        public ProductServiceImpl(IRepositoryContext context,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            ICategorizationRepository categorizationRepository,
            IDomainService domainService)
            : base(context)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _categorizationRepository = categorizationRepository;
            _domainService = domainService;
        }
        #endregion

        #region IProductService Members
        /// <summary>
        /// 创建商品信息。
        /// </summary>
        /// <param name="productDataObjects">需要创建的商品信息。</param>
        /// <returns>已创建的商品信息。</returns>
        public ProductDataObjectList CreateProducts(ProductDataObjectList productDataObjects)
        {
            return PerformCreateObjects<ProductDataObjectList, ProductDataObject, Product>(productDataObjects, _productRepository);
        }
        /// <summary>
        /// 创建商品分类。
        /// </summary>
        /// <param name="categoryDataObjects">需要创建的商品分类。</param>
        /// <returns>已创建的商品分类。</returns>
        public CategoryDataObjectList CreateCategories(CategoryDataObjectList categoryDataObjects)
        {
            return PerformCreateObjects<CategoryDataObjectList, CategoryDataObject, Category>(categoryDataObjects, _categoryRepository);
        }
        /// <summary>
        /// 更新商品信息。
        /// </summary>
        /// <param name="productDataObjects">需要更新的商品信息。</param>
        /// <returns>已更新的商品信息。</returns>
        public ProductDataObjectList UpdateProducts(ProductDataObjectList productDataObjects)
        {
            return PerformUpdateObjects<ProductDataObjectList, ProductDataObject, Product>(productDataObjects,
                _productRepository,
                pdo => pdo.ID,
                (p, pdo) =>
                {
                    if (!string.IsNullOrEmpty(pdo.Description))
                        p.Description = pdo.Description;
                    if (!string.IsNullOrEmpty(pdo.ImageUrl))
                        p.ImageUrl = pdo.ImageUrl;
                    if (!string.IsNullOrEmpty(pdo.Name))
                        p.Name = pdo.Name;
                    if (pdo.IsFeatured != null)
                        p.IsFeatured = pdo.IsFeatured.Value;
                    if (pdo.UnitPrice != null)
                        p.UnitPrice = pdo.UnitPrice.Value;
                });
        }
        /// <summary>
        /// 更新商品分类。
        /// </summary>
        /// <param name="categoryDataObjects">需要更新的商品分类。</param>
        /// <returns>已更新的商品分类。</returns>
        public CategoryDataObjectList UpdateCategories(CategoryDataObjectList categoryDataObjects)
        {
            return PerformUpdateObjects<CategoryDataObjectList, CategoryDataObject, Category>(categoryDataObjects,
                _categoryRepository,
                cdo => cdo.ID,
                (c, cdo) =>
                {
                    if (!string.IsNullOrEmpty(cdo.Name))
                        c.Name = cdo.Name;
                    if (!string.IsNullOrEmpty(cdo.Description))
                        c.Description = cdo.Description;
                });
        }
        /// <summary>
        /// 删除商品信息。
        /// </summary>
        /// <param name="productIDs">需要删除的商品信息的ID值。</param>
        public void DeleteProducts(IDList productIDs)
        {
            PerformDeleteObjects(productIDs,
                _productRepository,
                id =>
                {
                    var categorization = _categorizationRepository.Find(Specification<Categorization>.Eval(c => c.ProductID == id));
                    if (categorization != null)
                        _categorizationRepository.Remove(categorization);
                });
        }
        /// <summary>
        /// 删除商品分类。
        /// </summary>
        /// <param name="categoryIDs">需要删除的商品分类的ID值。</param>
        public void DeleteCategories(IDList categoryIDs)
        {
            PerformDeleteObjects(categoryIDs,
                _categoryRepository,
                id =>
                {
                    var categorization = _categorizationRepository.Find(Specification<Categorization>.Eval(c => c.CategoryID == id));
                    if (categorization != null)
                        _categorizationRepository.Remove(categorization);
                });
        }
        /// <summary>
        /// 设置商品分类。
        /// </summary>
        /// <param name="productID">需要进行分类的商品ID值。</param>
        /// <param name="categoryID">商品分类ID值。</param>
        /// <returns>带有商品分类信息的对象。</returns>
        public CategorizationDataObject CategorizeProduct(Guid productID, Guid categoryID)
        {
            if (productID == Guid.Empty)
                throw new ArgumentNullException("productID");
            if (categoryID == Guid.Empty)
                throw new ArgumentNullException("categoryID");
            var product = _productRepository.GetByKey(productID);
            var category = _categoryRepository.GetByKey(categoryID);
            return Mapper.Map<Categorization, CategorizationDataObject>(_domainService.Categorize(product, category));
        }
        /// <summary>
        /// 取消商品分类。
        /// </summary>
        /// <param name="productID">需要取消分类的商品ID值。</param>
        public void UncategorizeProduct(Guid productID)
        {
            if (productID == Guid.Empty)
                throw new ArgumentNullException("productID");
            var product = _productRepository.GetByKey(productID);
            _domainService.Uncategorize(product);
        }
        /// <summary>
        /// 根据指定的ID值获取商品分类。
        /// </summary>
        /// <param name="id">商品分类ID值。</param>
        /// <param name="spec">查询方式。</param>
        /// <returns>商品分类。</returns>
        public CategoryDataObject GetCategoryByID(Guid id, QuerySpec spec)
        {
            var category = _categoryRepository.GetByKey(id);
            var result = Mapper.Map<Category, CategoryDataObject>(category);
            if (spec != null && spec.IsVerbose())
            {
                var products = _categorizationRepository.GetProductsForCategory(category);
                var eProducts = products as IList<Product> ?? products.ToList();
                if (eProducts.Any())
                {
                    result.Products = new ProductDataObjectList();
                    eProducts
                        .ToList()
                        .ForEach(p => result.Products.Add(Mapper.Map<Product, ProductDataObject>(p)));
                }
            }
            return result;
        }
        /// <summary>
        /// 获取所有的商品分类。
        /// </summary>
        /// <param name="spec">查询方式。</param>
        /// <returns>所有的商品分类。</returns>
        public CategoryDataObjectList GetCategories(QuerySpec spec)
        {
            CategoryDataObjectList result = null;
            var categories = _categoryRepository.FindAll();
            if (categories != null)
            {
                result = new CategoryDataObjectList();
                foreach (var category in categories)
                {
                    var categoryDataObject = Mapper.Map<Category, CategoryDataObject>(category);
                    if (spec != null && spec.IsVerbose())
                    {
                        var products = _categorizationRepository.GetProductsForCategory(category);
                        var eProducts = products as IList<Product> ?? products.ToList();
                        if (products != null && eProducts.Any())
                        {
                            categoryDataObject.Products = new ProductDataObjectList();
                            eProducts.ToList()
                                .ForEach(p => categoryDataObject.Products.Add(Mapper.Map<Product, ProductDataObject>(p)));
                        }
                    }
                    result.Add(categoryDataObject);
                }
            }
            return result;
        }
        /// <summary>
        /// 根据指定的ID值获取商品信息。
        /// </summary>
        /// <param name="id">商品信息ID值。</param>
        /// <param name="spec">查询方式。</param>
        /// <returns>商品信息。</returns>
        public ProductDataObject GetProductByID(Guid id, QuerySpec spec)
        {
            var product = _productRepository.GetByKey(id);
            var result = Mapper.Map<Product, ProductDataObject>(product);
            if (spec != null && spec.IsVerbose())
            {
                result.Category = Mapper.Map<Category, CategoryDataObject>(_categorizationRepository.GetCategoryForProduct(product));
            }
            return result;
        }
        /// <summary>
        /// 获取所有的商品信息。
        /// </summary>
        /// <param name="spec">查询方式。</param>
        /// <returns>商品信息。</returns>
        public ProductDataObjectList GetProducts(QuerySpec spec)
        {
            var result = new ProductDataObjectList();
            _productRepository
                .FindAll()
                .ToList()
                .ForEach(p =>
                {
                    var dataObject = Mapper.Map<Product, ProductDataObject>(p);
                    if (spec != null && spec.IsVerbose())
                    {
                        var category = _categorizationRepository.GetCategoryForProduct(p);
                        if (category != null)
                        {
                            dataObject.Category = Mapper.Map<Category, CategoryDataObject>(category);
                        }
                    }
                    result.Add(dataObject);
                });
            return result;
        }
        /// <summary>
        /// 以分页的方式获取所有商品信息。
        /// </summary>
        /// <param name="pagination">带有分页参数信息的对象。</param>
        /// <returns>经过分页的商品信息。</returns>
        public ProductDataObjectListWithPagination GetProductsWithPagination(Pagination pagination)
        {
            var pagedProducts = _productRepository.FindAll(sp => sp.Name, SortOrder.Ascending, pagination.PageNumber, pagination.PageSize);
            pagination.TotalPages = pagedProducts.TotalPages;

            var productDataObjectList = new ProductDataObjectList();
            pagedProducts.Data.ToList().ForEach(p => productDataObjectList.Add(Mapper.Map<Product, ProductDataObject>(p)));
            return new ProductDataObjectListWithPagination
            {
                Pagination = pagination,
                ProductDataObjectList = productDataObjectList
            };
        }
        /// <summary>
        /// 根据指定的商品分类ID值，获取该分类下所有的商品信息。
        /// </summary>
        /// <param name="categoryID">商品分类ID值。</param>
        /// <returns>所有的商品信息。</returns>
        public ProductDataObjectList GetProductsForCategory(Guid categoryID)
        {
            var result = new ProductDataObjectList();
            var category = _categoryRepository.GetByKey(categoryID);
            var products = _categorizationRepository.GetProductsForCategory(category);
            products.ToList().ForEach(p => result.Add(Mapper.Map<Product, ProductDataObject>(p)));
            return result;
        }
        /// <summary>
        /// 根据指定的商品分类ID值，以分页的方式获取该分类下所有的商品信息。
        /// </summary>
        /// <param name="categoryID">商品分类ID值。</param>
        /// <param name="pagination">带有分页参数信息的对象。</param>
        /// <returns>所有的商品信息。</returns>
        public ProductDataObjectListWithPagination GetProductsForCategoryWithPagination(Guid categoryID, Pagination pagination)
        {
            var category = _categoryRepository.GetByKey(categoryID);
            var pagedProducts = _categorizationRepository.GetProductsForCategoryWithPagination(category, pagination.PageNumber, pagination.PageSize);
            if (pagedProducts == null)
            {
                pagination.TotalPages = 0;
                return new ProductDataObjectListWithPagination
                {
                    Pagination = pagination,
                    ProductDataObjectList = new ProductDataObjectList()
                };
            }
            pagination.TotalPages = pagedProducts.TotalPages;
            var productDataObjectList = new ProductDataObjectList();
            pagedProducts.Data.ToList().ForEach(p => productDataObjectList.Add(Mapper.Map<Product, ProductDataObject>(p)));
            return new ProductDataObjectListWithPagination
            {
                Pagination = pagination,
                ProductDataObjectList = productDataObjectList
            };
        }
        /// <summary>
        /// 获取所有的特色商品信息。
        /// </summary>
        /// <param name="count">需要获取的特色商品信息的个数。</param>
        /// <returns>特色商品信息。</returns>
        public ProductDataObjectList GetFeaturedProducts(int count)
        {
            var featuredProducts = _productRepository.GetFeaturedProducts(count);
            var result = new ProductDataObjectList();
            featuredProducts
                .ToList()
                .ForEach(fp => result.Add(Mapper.Map<Product, ProductDataObject>(fp)));
            return result;
        }

        #endregion
    }
}
