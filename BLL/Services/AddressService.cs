using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _repo;
        public AddressService(IAddressRepository repo) => _repo = repo;

        public async Task<List<AddressDto>> ListAsync(int accountId)
        {
            var list = await _repo.ListAsync(accountId);
            return list.Select(Map).ToList();
        }

        public async Task<AddressDto?> GetAsync(int accountId, int addressId)
        {
            var a = await _repo.GetAsync(accountId, addressId);
            return a == null ? null : Map(a);
        }

        public async Task<int> CreateAsync(int accountId, string city, string ward, string addressLine, bool setAsDefault)
        {
            AddressValidator.Validate(city, ward, addressLine);
            var existing = await _repo.ListAsync(accountId);
            if (existing.Count >= 5)
                throw new InvalidOperationException("Bạn chỉ được thêm tối đa 5 địa chỉ.");

            addressLine = (addressLine ?? "").Trim();
            city = (city ?? "").Trim();
            ward = (ward ?? "").Trim();

            int count = await _repo.CountActiveAsync(accountId);
            bool makeDefault = count == 0 || setAsDefault;

            if (makeDefault)
            {
                var all = await _repo.GetAllForAccountAsync(accountId);
                foreach (var x in all.Where(x => x.IsDefault))
                {
                    x.IsDefault = false;
                    await _repo.UpdateAsync(x);
                }
            }

            var entity = new Address
            {
                AccountId = accountId,
                City = city,
                Ward = string.IsNullOrWhiteSpace(ward) ? null : ward,
                AddressLine = addressLine,
                IsDefault = makeDefault,
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(entity);
            return entity.AddressId;
        }

        public async Task<bool> UpdateAsync(int accountId, int addressId, string city, string ward, string addressLine, bool setAsDefault)
        {
            AddressValidator.Validate(city, ward, addressLine);
            var entity = await _repo.GetAsync(accountId, addressId);
            if (entity == null) return false;

            entity.City = (city ?? "").Trim();
            entity.Ward = string.IsNullOrWhiteSpace(ward) ? null : ward.Trim();
            entity.AddressLine = (addressLine ?? "").Trim();
            entity.UpdatedAt = DateTime.Now;

            if (setAsDefault && !entity.IsDefault)
            {
                var all = await _repo.GetAllForAccountAsync(accountId);
                foreach (var x in all)
                {
                    bool shouldBeDefault = x.AddressId == addressId;
                    if (x.IsDefault != shouldBeDefault)
                    {
                        x.IsDefault = shouldBeDefault;
                        await _repo.UpdateAsync(x);
                    }
                }
            }
            else
            {
                await _repo.UpdateAsync(entity);
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int accountId, int addressId)
        {
            var entity = await _repo.GetAsync(accountId, addressId);
            if (entity == null) return false;

            bool wasDefault = entity.IsDefault;

            await _repo.DeleteAsync(entity);

            if (wasDefault)
            {
                var remain = await _repo.GetAllForAccountAsync(accountId);
                var next = remain
                    .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                    .FirstOrDefault();

                if (next != null && !next.IsDefault)
                {
                    next.IsDefault = true;
                    next.UpdatedAt = DateTime.Now;
                    await _repo.UpdateAsync(next);
                }
            }

            return true;
        }

        public async Task<bool> SetDefaultAsync(int accountId, int addressId)
        {
            var all = await _repo.GetAllForAccountAsync(accountId);
            bool changed = false;

            foreach (var a in all)
            {
                bool shouldBeDefault = a.AddressId == addressId;
                if (a.IsDefault != shouldBeDefault)
                {
                    a.IsDefault = shouldBeDefault;
                    a.UpdatedAt = DateTime.Now;
                    await _repo.UpdateAsync(a);
                    changed = true;
                }
            }
            return changed;
        }

        private static AddressDto Map(Address a) => new()
        {
            Id = a.AddressId,
            AccountId = a.AccountId,
            City = a.City,
            Ward = a.Ward,
            AddressLine = a.AddressLine,
            IsDefault = a.IsDefault,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
