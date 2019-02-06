﻿using BookingApp.Data;
using BookingApp.Data.Models;
using BookingApp.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BookingApp.Services
{
    public class TreeGroupService
    {
        TreeGroupRepository repository;
        readonly UserManager<ApplicationUser> userManager;

        public TreeGroupService(TreeGroupRepository r, UserManager<ApplicationUser> userManager)
        {
            repository = r;
            this.userManager = userManager;
        }

        public async Task<IEnumerable<TreeGroup>> GetThree()
        {
            return await repository.GetListAsync();
        }

        public async Task<TreeGroup> GetDetail(int id)
        {
            return await repository.GetAsync(id);
        }

        public async Task Create(TreeGroup tree)
        {
            await repository.CreateAsync(tree);
        }

        public async Task Update(int id, TreeGroup tree)
        {
            tree.TreeGroupId = id;
            await repository.UpdateAsync(tree);
        }

        public async Task Delete(int id) => await repository.DeleteAsync(id);
    }
}
