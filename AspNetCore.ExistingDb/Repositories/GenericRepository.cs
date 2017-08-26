using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Repositories
{
	public interface IGenericRepository<Cont, Ent>
		where Ent : class
		where Cont : DbContext
	{
		//Cont Context { get; }

		EntityEntry<Ent> Add(Ent entity);
		Task<EntityEntry<Ent>> AddAsync(Ent entity);
		Task AddRangeAsync(IEnumerable<Ent> entities);
		void Delete(Ent entity);
		void DeleteRange(IEnumerable<Ent> entities);
		void DeleteAll();
		void Edit(Ent entity);
		IQueryable<Ent> FindBy(Expression<Func<Ent, bool>> predicate);
		Task<List<Ent>> FindByAsync(Expression<Func<Ent, bool>> predicate);
		Task<Ent> GetSingleAsync(params object[] keyValues);
		IQueryable<Ent> GetAll();
		Task<List<Ent>> GetAllAsync();
		int Save();
		Task<int> SaveAsync();
	}

	public abstract class GenericRepository<Cont, Ent> : IGenericRepository<Cont, Ent>
		where Ent : class
		where Cont : DbContext
	{
		protected Cont _entities;

		/*public Cont Context
		{
			get { return _entities; }
		}*/

		protected static IEnumerable<string> AllColumnNames
		{
			get
			{
				return typeof(Ent).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);
			}
		}

		public GenericRepository(Cont context)
		{
			_entities = context;
		}

		public virtual IQueryable<Ent> GetAll()
		{
			IQueryable<Ent> query = _entities.Set<Ent>();
			return query;
		}

		public virtual async Task<List<Ent>> GetAllAsync()
		{
			var tsk = _entities.Set<Ent>().ToListAsync();
			return await tsk;
		}

		public virtual IQueryable<Ent> FindBy(Expression<Func<Ent, bool>> predicate)
		{
			IQueryable<Ent> query = _entities.Set<Ent>().Where(predicate);
			return query;
		}

		public virtual async Task<List<Ent>> FindByAsync(Expression<Func<Ent, bool>> predicate)
		{
			var query = _entities.Set<Ent>().Where(predicate).ToListAsync();
			return await query;
		}

		public virtual async Task<Ent> GetSingleAsync(params object[] keyValues)
		{
			var query = _entities.Set<Ent>().FindAsync(keyValues);
			return await query;
		}

		public virtual EntityEntry<Ent> Add(Ent entity)
		{
			return _entities.Set<Ent>().Add(entity);
		}

		public virtual async Task<EntityEntry<Ent>> AddAsync(Ent entity)
		{
			return await _entities.Set<Ent>().AddAsync(entity);
		}

		public virtual Task AddRangeAsync(IEnumerable<Ent> entities)
		{
			return _entities.Set<Ent>().AddRangeAsync(entities);
		}

		public virtual void Delete(Ent entity)
		{
			_entities.Set<Ent>().Remove(entity);
		}

		public virtual void DeleteRange(IEnumerable<Ent> entities)
		{
			_entities.Set<Ent>().RemoveRange(entities);
		}

		public virtual void DeleteAll()
		{
			IQueryable<Ent> query = _entities.Set<Ent>();
			_entities.Set<Ent>().RemoveRange(query);
		}

		public virtual void Edit(Ent entity)
		{
			_entities.Entry(entity).State = EntityState.Modified;
		}

		public virtual int Save()
		{
			return _entities.SaveChanges();
		}

		public virtual async Task<int> SaveAsync()
		{
			return await _entities.SaveChangesAsync();
		}
	}
}
