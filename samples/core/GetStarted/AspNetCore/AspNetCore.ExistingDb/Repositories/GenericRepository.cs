using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
		void Delete(Ent entity);
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

		public GenericRepository(Cont context)
		{
			_entities = context;
		}

		public virtual IQueryable<Ent> GetAll()
		{
			IQueryable<Ent> query = _entities.Set<Ent>();
			return query;
		}

		public virtual Task<List<Ent>> GetAllAsync()
		{
			var tsk = _entities.Set<Ent>().ToListAsync();
			return tsk;
		}

		public virtual IQueryable<Ent> FindBy(Expression<Func<Ent, bool>> predicate)
		{
			IQueryable<Ent> query = _entities.Set<Ent>().Where(predicate);
			return query;
		}

		public virtual Task<List<Ent>> FindByAsync(Expression<Func<Ent, bool>> predicate)
		{
			var query = _entities.Set<Ent>().Where(predicate).ToListAsync();
			return query;
		}

		public virtual Task<Ent> GetSingleAsync(params object[] keyValues)
		{
			var query = _entities.Set<Ent>().FindAsync(keyValues);
			return query;
		}

		public virtual EntityEntry<Ent> Add(Ent entity)
		{
			return _entities.Set<Ent>().Add(entity);
		}

		public virtual Task<EntityEntry<Ent>> AddAsync(Ent entity)
		{
			return _entities.Set<Ent>().AddAsync(entity);
		}

		public virtual void Delete(Ent entity)
		{
			_entities.Set<Ent>().Remove(entity);
		}

		public virtual void Edit(Ent entity)
		{
			_entities.Entry(entity).State = EntityState.Modified;
		}

		public virtual int Save()
		{
			return _entities.SaveChanges();
		}

		public virtual Task<int> SaveAsync()
		{
			return _entities.SaveChangesAsync();
		}
	}
}
