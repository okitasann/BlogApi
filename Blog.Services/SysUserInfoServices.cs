﻿using System.Linq;
using System.Threading.Tasks;
using Blog.IRepository;
using Blog.IServices;
using Blog.Model.Models;
using Blog.Services.Base;

namespace Blog.Services
{
    /// <summary>
    /// sysUserInfoServices
    /// </summary>	
    public class SysUserInfoServices : BaseServices<sysUserInfo>, ISysUserInfoServices
    {

        IsysUserInfoRepository _dal;
        IUserRoleServices _userRoleServices;
        IRoleRepository _roleRepository;
        public SysUserInfoServices(IsysUserInfoRepository dal, IUserRoleServices userRoleServices, IRoleRepository roleRepository)
        {
            this._dal = dal;
            this._userRoleServices = userRoleServices;
            this._roleRepository = roleRepository;
            base.BaseDal = dal;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginId"></param>   
        /// <param name="loginPwd"></param>
        /// <param name="uname"></param>
        /// <param name="desc"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public async Task<sysUserInfo> SaveUserInfo(string loginId, string loginPwd, string Uname = "", string desc = "", string title = "")
        {
            sysUserInfo sysUserInfo = new sysUserInfo(loginId, loginPwd, Uname, desc, title);
            sysUserInfo model = new sysUserInfo();
            var userList = await base.Query(a => a.uLoginId == sysUserInfo.uLoginId && a.uLoginPwd == sysUserInfo.uLoginPwd);
            if (userList.Count > 0)
            {
                model = userList.FirstOrDefault();
            }
            else
            {
                var id = await base.Add(sysUserInfo);
                model = await base.QueryById(id);
            }

            return model;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="loginPwd"></param>
        /// <returns></returns>
        public async Task<string> GetUserRoleNameStr(string loginName, string loginPwd)
        {
            string roleName = "";
            var user = (await base.Query(a => a.uLoginId == loginName && a.uLoginPwd == loginPwd)).FirstOrDefault();
            var roleList = await _roleRepository.Query(a => a.IsDeleted == false);
            if (user != null)
            {
                var userRoles = await _userRoleServices.Query(ur => ur.UserId == user.uID);
                if (userRoles.Count > 0)
                {
                    var arr = userRoles.Select(ur => ur.RoleId.ObjToString()).ToList();
                    var roles = roleList.Where(d => arr.Contains(d.Id.ObjToString()));

                    roleName = string.Join(',', roles.Select(r => r.Name).ToArray());
                }
            }
            return roleName;
        }
    }
}

