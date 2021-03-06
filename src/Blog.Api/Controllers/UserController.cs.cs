using System;
using System.Linq;
using System.Threading.Tasks;
using Blog.Api.Common.Helper;
using Blog.Api.Common.HttpContextUser;
using Blog.AuthHelper.OverWrite;
using Blog.IRepository.IUnitOfWork;
using Blog.IServices;
using Blog.Model;
using Blog.Model.Models;
using Blog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blog.Api.Controllers
{
    /// <summary>
    /// 用户管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    //[Route("api/user")]
    [ApiController]
    [Authorize(Permissions.Name)]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        readonly ISysUserInfoServices _sysUserInfoServices;
        readonly IUserRoleServices _userRoleServices;
        readonly IRoleServices _roleServices;
        private readonly IUser _user;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="sysUserInfoServices"></param>
        /// <param name="userRoleServices"></param>
        /// <param name="roleServices"></param>
        /// <param name="user"></param>
        /// <param name="logger"></param>
        public UserController(IUnitOfWork unitOfWork, ISysUserInfoServices sysUserInfoServices, IUserRoleServices userRoleServices, IRoleServices roleServices, IUser user, ILogger<UserController> logger)
        {
            _unitOfWork = unitOfWork;
            _sysUserInfoServices = sysUserInfoServices;
            _userRoleServices = userRoleServices;
            _roleServices = roleServices;
            _user = user;
            _logger = logger;
        }


        /// <summary>
        /// 获取用户详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<object> Get(int id)
        {
            var result = new MessageModel<sysUserInfo>();
            var sysUserInfos = await _sysUserInfoServices.QueryById(id);
            if (sysUserInfos == null)
            {
                return result;
            }
            #region MyRegion

            // 这里可以封装到多表查询，此处简单处理
            var roleIds = await _userRoleServices.GetRoleIdByUid(id);
            var roleName = await _roleServices.GetRoleNameByRid(roleIds.Select(x => x as object).ToArray());

            sysUserInfos.RIDs = roleIds;
            sysUserInfos.RoleNames = roleName;

            #endregion
            result.msg = "success";
            result.success = true;
            result.response = sysUserInfos;
            return result;
        }

        // GET: api/User/5
        /// <summary>
        /// 获取用户详情根据token
        /// 【无权限】
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<sysUserInfo>> GetInfoByToken(string token)
        {
            var data = new MessageModel<sysUserInfo>();
            if (!string.IsNullOrEmpty(token))
            {
                var tokenModel = JwtHelper.SerializeJwt(token);
                if (tokenModel != null && tokenModel.Uid > 0)
                {
                    var userinfo = await _sysUserInfoServices.QueryById(tokenModel.Uid);
                    // 这里可以封装到多表查询，此处简单处理
                    var roleIds = await _userRoleServices.GetRoleIdByUid((int)tokenModel.Uid);
                    var roleName = await _roleServices.GetRoleNameByRid(roleIds.Select(x => x as object).ToArray());

                    userinfo.RIDs = roleIds;
                    userinfo.RoleNames = roleName;
                    if (userinfo != null)
                    {
                        data.response = userinfo;
                        data.success = true;
                        data.msg = "获取成功";
                    }
                }

            }
            return data;
        }

        /// <summary>
        /// 获取全部用户
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<PageModel<sysUserInfo>>> Get(int page = 1, int pageSize = 25, string key = "")
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                key = "";
            }

            var data = await _sysUserInfoServices.QueryPage(a => a.tdIsDelete != true && a.uStatus >= 0 && ((a.uEmail != null && a.uEmail.Contains(key)) || (a.uName != null && a.uName.Contains(key))), page, pageSize, " uID desc ");

            // 这里可以封装到多表查询，此处简单处理
            var allUserRoles = await _userRoleServices.Query(d => d.IsDeleted == false);
            var allRoles = await _roleServices.Query(d => d.IsDeleted == false);

            var sysUserInfos = data.data;
            foreach (var item in sysUserInfos)
            {
                var currentUserRoles = allUserRoles.Where(d => d.UserId == item.uId).Select(d => d.RoleId).ToList();
                item.RIDs = currentUserRoles;
                item.RoleNames = allRoles.Where(d => currentUserRoles.Contains(d.Id)).Select(d => d.Name).ToList();
            }
            data.data = sysUserInfos;


            return new MessageModel<PageModel<sysUserInfo>>()
            {
                msg = "success",
                success = data.dataCount >= 0,
                response = data
            };


        }
        /// <summary>
        /// 添加一个用户
        /// </summary>
        /// <param name="sysUserInfo"></param>
        /// <returns></returns>
        // POST: api/User
        [HttpPost]
        [AllowAnonymous]
        public async Task<MessageModel<string>> Post([FromBody] sysUserInfo sysUserInfo)
        {
            var data = new MessageModel<string>();

            sysUserInfo.uPassword = MD5Helper.MD5Encrypt32(sysUserInfo.uPassword);
            sysUserInfo.uRemark = _user.Name;

            var uId = await _sysUserInfoServices.Add(sysUserInfo);
            await _userRoleServices.Add(new UserRole(uId));

            data.success = uId > 0;
            if (data.success)
            {
                data.response = uId.ObjToString();
                data.msg = "Add user successfully.";
            }

            return data;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="sysUserInfo"></param>
        /// <returns></returns>
        // PUT: api/User/5
        [HttpPut]
        public async Task<MessageModel<string>> Put([FromBody] sysUserInfo sysUserInfo)
        {
            var data = new MessageModel<string>();
            if (sysUserInfo == null || sysUserInfo.uId <= 0)
            {
                return data;
            }

            var sysUser = (await _sysUserInfoServices.Query(d => d.uId == sysUserInfo.uId)).FirstOrDefault();
            sysUser.uDescription = sysUserInfo.uDescription ?? sysUser.uDescription;
            sysUser.uTitle = sysUserInfo.uTitle ?? sysUser.uTitle;
            sysUser.uPassword = string.IsNullOrEmpty(sysUserInfo.uPassword) ? sysUser.uPassword : MD5Helper.MD5Encrypt32(sysUserInfo.uPassword);

            try
            {
                _unitOfWork.BeginTran();
                // 无论 Update Or Add , 先删除当前用户的全部 U_R 关系
                // var usreroles = (await _userRoleServices.Query(d => d.UserId == sysUser.uId)).Select(d => d.Id.ToString()).ToArray();
                // if (usreroles.Count() > 0)
                // {
                //     var isAllDeleted = await _userRoleServices.DeleteByIds(usreroles);
                // }

                // // 然后再执行添加操作
                // var userRolsAdd = new List<UserRole>();
                // sysUser.RIDs.ForEach(rid =>
                // {
                //     userRolsAdd.Add(new UserRole(sysUser.uId, rid));
                // });

                // await _userRoleServices.Add(userRolsAdd);

                data.success = await _sysUserInfoServices.Update(sysUser);
                _unitOfWork.CommitTran();

                if (data.success)
                {
                    data.msg = "Update userInfo successfully.";
                    data.response = sysUser?.uId.ObjToString();
                }

            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTran();
                _logger.LogError(ex, ex.Message);
            }

            return data;

        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ApiWithActions/5
        [HttpDelete]
        public async Task<MessageModel<string>> Delete(int id)
        {
            var data = new MessageModel<string>();
            if (id < 0)
            {
                data.msg = "Invalid user id.";
                data.response = id.ToString();
            };
            var userDetail = await _sysUserInfoServices.QueryById(id);
            userDetail.tdIsDelete = true;
            data.success = await _sysUserInfoServices.Update(userDetail);
            if (data.success)
            {
                data.msg = "Delete successfully.";
                data.response = userDetail?.uId.ObjToString();
            }
            return data;

        }
    }
}
