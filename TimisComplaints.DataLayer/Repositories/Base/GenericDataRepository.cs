﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TimisComplaints.DataLayer.Interfaces;

namespace TimisComplaints.DataLayer.Repositories.Base
{
    public abstract class GenericDataRepository<T> : IDisposable
        where T : class, IEntity, new()
    {
        private Entities mContext;
        private DbSet<T> mDbSet;

        private bool mIsEntityTrackingOn;
        private Func<IList<string>, IQueryable<T>> mQueryGenerator;

        protected GenericDataRepository()
        {
            Context = new Entities();
            IsEntityTrackingOn = false;
        }

        protected Entities Context
        {
            get { return mContext; }
            set
            {
                if (value == null)
                {
                    throw new NullReferenceException("Tried to assign null context");
                }

                value.Configuration.LazyLoadingEnabled = false;
                mContext = value;
                mDbSet = mContext.Set<T>();
            }
        }

        public bool IsEntityTrackingOn
        {
            get { return mIsEntityTrackingOn; }
            set
            {
                mIsEntityTrackingOn = value;

                mQueryGenerator = mIsEntityTrackingOn ? (Func<IList<string>, IQueryable<T>>)GenerateQuery : GenerateNonTrackingQuery;
            }
        }

        protected async Task<IList<T>> FetchAllAsync(IList<string> navigationProperties = null)
        {
            var dbQuery = mQueryGenerator.Invoke(navigationProperties);

            var list = await dbQuery.ToListAsync();
            return list;
        }

        protected async Task<IList<T>> FetchListAsync(Expression<Func<T, bool>> where, IList<string> navigationProperties = null)
        {
            var dbQuery = mQueryGenerator.Invoke(navigationProperties);

            var list = await dbQuery.Where(@where).ToListAsync();

            return list;
        }

        protected async Task<T> FetchSingleAsync(Expression<Func<T, bool>> where, IList<string> navigationProperties = null)
        {
            var dbQuery = mQueryGenerator.Invoke(navigationProperties);

            var item = await dbQuery.FirstOrDefaultAsync(@where);

            return item;
        }

        protected async Task<T> AddAsync(T item)
        {
            mDbSet.Add(item);

            await mContext.SaveChangesAsync();

            return item;
        }

        protected async Task<IList<T>> AddAsync(IList<T> items)
        {
            mDbSet.AddRange(items);

            await mContext.SaveChangesAsync();

            return items;
        }

        protected async Task<T> ChangeAsync(T item)
        {
            mContext.Entry(item).State = EntityState.Modified;

            await mContext.SaveChangesAsync();

            return item;
        }

        protected async Task<IList<T>> ChangeAsync(IList<T> items)
        {
            mContext.Configuration.AutoDetectChangesEnabled = false;

            foreach (var item in items)
            {
                mContext.Entry(item).State = EntityState.Modified;
            }

            mContext.Configuration.AutoDetectChangesEnabled = true;
            await mContext.SaveChangesAsync();

            return items;
        }

        protected async Task RemoveAsync(T item)
        {
            if (mContext.Entry(item).State == EntityState.Detached)
            {
                mDbSet.Attach(item);
            }

            mDbSet.Remove(item);

            await mContext.SaveChangesAsync();
        }

        protected async Task RemoveAsync(IList<T> items)
        {
            mContext.Configuration.AutoDetectChangesEnabled = false;

            foreach (var item in items)
            {
                if (mContext.Entry(item).State == EntityState.Detached)
                {
                    mDbSet.Attach(item);
                }
            }

            mContext.Configuration.AutoDetectChangesEnabled = true;

            mDbSet.RemoveRange(items);

            await mContext.SaveChangesAsync();
        }

        #region Private methods

        private IQueryable<T> GenerateNonTrackingQuery(IList<string> navigationProperties)
        {
            IQueryable<T> dbQuery = mDbSet.AsNoTracking();

            return ApplyNavigationProperties(dbQuery, navigationProperties);
        }

        private IQueryable<T> GenerateQuery(IList<string> navigationProperties)
        {
            IQueryable<T> dbQuery = mDbSet;

            return ApplyNavigationProperties(dbQuery, navigationProperties);
        }

        private static IQueryable<T> ApplyNavigationProperties(IQueryable<T> dbQuery, IList<string> navigationProperties)
        {
            if (navigationProperties != null)
            {
                dbQuery = navigationProperties.Aggregate(dbQuery, (current, navigationProperty) => current.Include(navigationProperty));
            }

            return dbQuery;
        }

        #endregion

        #region Disposing logic

        public void Dispose()
        {
            mContext.Dispose();
        }

        #endregion
    }
}