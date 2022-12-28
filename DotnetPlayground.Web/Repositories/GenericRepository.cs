using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetPlayground.Repositories
{
	public interface IGenericRepository<Cont, Ent>
		where Ent : class
		where Cont : DbContext
	{
		//Cont Context { get; }

		Ent Add(Ent entity);
		Task<Ent> AddAsync(Ent entity);
		Task AddRangeAsync(IEnumerable<Ent> entities);
		Task<bool> Delete(Expression<Func<Ent, bool>> predicate);
		void DeleteRange(IEnumerable<Ent> entities);
		void DeleteAll();
		Task<int> Edit(Expression<Func<Ent, bool>> predicate,
            Expression<Func<SetPropertyCalls<Ent>, SetPropertyCalls<Ent>>> setPropertyCalls);
		IQueryable<Ent> FindBy(Expression<Func<Ent, bool>> predicate);
		Task<List<Ent>> FindByAsync(Expression<Func<Ent, bool>> predicate);
		Task<Ent> GetSingleAsync(params object[] keyValues);
		IQueryable<Ent> GetAll();
		Task<List<Ent>> GetAllAsync();
		Task<List<Ent>> GetAllAsync(string include);
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

		public virtual async Task<List<Ent>> GetAllAsync(string include)
		{
			var tsk = _entities.Set<Ent>().Include(include).ToListAsync();
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

		public virtual Ent Add(Ent entity)
		{
			var ent_entry = _entities.Set<Ent>().Add(entity);
			return ent_entry.Entity;
		}

		public virtual async Task<Ent> AddAsync(Ent entity)
		{
			var ent_entry = await _entities.Set<Ent>().AddAsync(entity);
			return ent_entry.Entity;
		}

		public virtual Task AddRangeAsync(IEnumerable<Ent> entities)
		{
			return _entities.Set<Ent>().AddRangeAsync(entities);
		}

		public virtual async Task<bool> Delete(Expression<Func<Ent, bool>> predicate)
		{
            var deleted_count = await _entities.Set<Ent>().Where(predicate).ExecuteDeleteAsync();
            if (deleted_count > 0)
                return true;
            return false;
        }

        public virtual void DeleteRange(IEnumerable<Ent> entities)
		{
			_entities.Set<Ent>().RemoveRange(entities);
		}

		public virtual void DeleteAll()
		{
			_entities.Set<Ent>().ExecuteDelete();
		}

		public virtual async Task<int> Edit(Expression<Func<Ent, bool>> predicate,
            Expression<Func<SetPropertyCalls<Ent>, SetPropertyCalls<Ent>>> setPropertyCalls
            )
		{
			//_entities.Entry(entity).State = EntityState.Modified;

			var updated_count = await _entities.Set<Ent>()
				.Where(predicate)
                .ExecuteUpdateAsync(setPropertyCalls);

			return updated_count;
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
