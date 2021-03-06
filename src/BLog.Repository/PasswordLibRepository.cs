﻿using Blog.IRepository;
using Blog.IRepository.IUnitOfWork;
using Blog.Model.Models;
using Blog.Repository.Base;

namespace Blog.Repository
{
    public partial class PasswordLibRepository : BaseRepository<PasswordLib>, IPasswordLibRepository
    {
        public PasswordLibRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
