﻿var getModels = function (path, f) {
    $.get("/puck/api/models?p_path=" + path, f);
};
var sortNodes = function (id, items, f) {
    var items_str = "";
    $(items).each(function (i) {
        items_str += "items=" + this + "&";
    });
    items_str = items_str.substring(0, items_str.length - 1);
    $.ajax({
        url: "/puck/api/sort?parentId=" + id,
        data: items_str,
        traditional: true,
        success: f,
        type: "POST",
        datatype: "json"
    });
}
var getSearchTypes = function (root,f) {
    $.get("/puck/api/searchtypes?root=" + root, function (d) {
        f(d);
    });
}
var getMarkup = function (parentId, type, variant, f, fromVariant, contentId) {
    $.get("/puck/api/edit?" + (parentId == null ? "" : "parentId=" + parentId + "&")
        + (contentId == null ? "" : "contentId=" + contentId + "&")
        + "&p_variant=" + variant + "&p_type=" + (type==null?"":type) + /*"&p_path=" + path +*/ "&p_fromVariant=" + (fromVariant == undefined ? "" : fromVariant), f, "html");
}
var getPrepopulatedMarkup = function (type,id,f) {
    $.get("/puck/api/prepopulatedEdit?p_type="+(type==null?"":type)+"&id="+id, f, "html");
}
var getAuditMarkup = function (id, variant, username, page, pageSize, f) {
    $.get("/puck/api/auditMarkup?id=" + id + "&variant=" + (variant || "") + "&username=" + (username || "") + "&page=" + page + "&pageSize=" + pageSize, f, "html");
}
var getTimedPublishDialog = function (id,variant, f) {
    $.get("/puck/api/timedpublishdialog?id=" + id+"&variant="+variant, f, "html");
}
var getCreateDialog = function (f, t) {
    $.get("/puck/api/createdialog" + (t === undefined ? "" : "?type=" + t), f, "html");
}
var getChangeTypeDialog = function (id,f) {
    $.get("/puck/api/changetypedialog?id="+id,f,"html");
}
var getChangeTypeMappingDialog = function (id,newType, f) {
    $.get("/puck/api/changetypemappingdialog?id=" + id+"&newType="+newType, f, "html");
}
var getTemplateCreateDialog = function (f, p) {
    $.get("/puck/task/createtemplate" + (p === undefined ? "" : "?path=" + p), f, "html");
}
var getTemplateFolderCreateDialog = function (f, p) {
    $.get("/puck/task/createfolder" + (p === undefined ? "" : "?path=" + p), f, "html");
}
var getSettings = function (path, f) {
    if (path == undefined) path = "/puck/settings/edit";
    $.get(path, f, "html");
}
var getVariants = function (f) {
    $.get("/puck/api/variants", f);
}
var getVariantsForPath = function (path,f) {
    $.get("/puck/api/variantsfornode/?path="+path, f);
}
var getVariantsForId = function (id, f) {
    $.get("/puck/api/variantsfornodebyid/?id=" + id, f);
}
var setDeleteTemplate = function (p, f) {
    $.post("/puck/task/deletetemplate?path=" + p, f);
}
var setDeleteTemplateFolder = function (p, f) {
    $.post("/puck/task/deletetemplatefolder?path=" + p, f);
}
var setPublish = function (id, variant, descendants, f) {
    var path = "/puck/api/publish?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    if (descendants != undefined)
        path += "&descendants=" + descendants;
    $.get(path, f);
}
var setUnpublish = function (id, variant, descendants, f) {
    var path = "/puck/api/unpublish?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    if (descendants != undefined)
        path += "&descendants=" + descendants;
    $.get(path, f);
}
var setDelete = function (id, f, variant) {
    var path = "/puck/api/delete?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    $.get(path, f);
}
var setMoveTemplate = function (from, to, f) {
    var path = "/puck/task/movetemplate?from=" + from + "&to=" + to;
    $.post(path, f);
}
var setMoveTemplateFolder = function (from, to, f) {
    var path = "/puck/task/movetemplatefolder?from=" + from + "&to=" + to;
    $.post(path, f);
}
var setMove = function (from, to, f) {
    var path = "/puck/api/move?startId=" + from + "&destinationId=" + to;
    $.get(path, f);
}
var setCopy = function (id, parentId, includeDescendants, f) {
    var path = "/puck/api/copy?id=" + id + "&parentId=" + parentId + "&includeDescendants=" + includeDescendants;
    $.get(path, f);
}
var setDeleteRevision = function (id, f) {
    var path = "/puck/api/deleterevision?id=" + id;
    $.get(path, f);
}
var setRevert = function (id, f) {
    var path = "/puck/api/revert?id=" + id;
    $.get(path, f);
}
var getFieldGroups = function (t, f) {
    $.get("/puck/api/fieldgroups?type=" + t, f);
}
var getNotifyDialog = function (p, f) {
    $.get("/puck/api/NotifyDialog?p_path=" + p, f);
}
var getLocalisationDialog = function (p, f) {
    $.get("/puck/api/LocalisationDialog?p_path=" + p, f);
}
var getDomainMappingDialog = function (p, f) {
    $.get("/puck/api/DomainMappingDialog?p_path=" + p, f);
}
var getTasks = function (f) {
    $.get("/puck/task/index", f);
}
var getTaskCreateDialog = function (f) {
    $.get("/puck/task/CreateTaskDialog", f);
}
var getTaskMarkup = function (f, type, id) {
    var type = isNullEmpty(type) ? "" : ("type=" + type);
    var id = isNullEmpty(id) ? "" : "id=" + id;
    if (!isNullEmpty(type))
        type += "&"
    $.get("/puck/task/Edit?" + type + id, f);
}
var getUsers = function (f) {
    $.get("/puck/admin/index", f, "html");
}
var getUserMarkup = function (u, f) {
    $.get("/puck/admin/edit?username=" + u, f, "html");
}
var setDeleteUser = function (u, f) {
    $.get("/puck/admin/delete?username=" + u, f);
}
var getRevisions = function (id, variant, f) {
    $.get("/puck/api/revisions?id=" + id + "&variant=" + variant, f, "html");
}
var getCompareMarkup = function (id, f) {
    $.get("/puck/api/compare?id=" + id, f, "html");
}
var getCacheInfo = function (path, f) {
    $.get("/puck/api/cacheinfo?p_path=" + path, f);
}
var getUserRoles = function (f) {
    $.get("/puck/api/userroles", f);
}
var getUserLanguage = function (f) {
    $.get("/puck/api/userlanguage", f);
}
var setCacheInfo = function (path, value, f) {
    $.post("/puck/api/cacheinfo?p_path=" + path + "&value=" + value, f);
}
var deleteParameters = function (key, f) {
    $.get("/puck/settings/DeleteParameters?key=" + key, function (data) {
        if (data.success) {
            f();
        } else {
            msg(false, data.message);
        }
    });
}
var getEditorParametersMarkup = function (f, settingsType, modelType, propertyName) {
    $.get("/puck/settings/EditParameters?settingsType=" + settingsType + "&modelType=" + modelType + "&propertyName=" + propertyName, f);
}
var getContent = function (path, f) {
    $.get("/puck/api/content?path=" + path, f);
}
var getContentByParentId = function (parentId, f, cast) {
    if (cast == undefined) cast = true;
    $.get("/puck/api/contentbyparentid?cast="+cast+"&parentid=" + parentId, f);
}

var getTemplates = function (path, f) {
    $.get("/puck/task/templates?path=" + path, f);
}
var getPath = function (id, f) {
    $.get("/puck/api/getpath?id=" + id, f);
};
var getIdPath = function (id, f) {
    $.get("/puck/api/getidpath?id=" + id, f);
};
var getStartPath = function (f) {
    $.get("/puck/api/startpath", f);
};
var getStartId = function (f) {
    $.get("/puck/api/startid", f);
};
var getSearchView = function (term, f, type, root) {
    $.get("/puck/api/searchview?q=" + term + "&type=" + type + "&root=" + root, f, "html");
}
var getSearch = function (term, f, type, root) {
    $.get("/puck/api/search?q=" + term + "&type=" + type + "&root=" + root, f);
}
var setRepublishEntireSite = function (f) {
    $.post("/puck/api/RepublishEntireSite", f);
}
var getRepublishEntireSiteStatus = function (f) {
    $.get("/puck/api/GetRepublishEntireSiteStatus", f);
}
