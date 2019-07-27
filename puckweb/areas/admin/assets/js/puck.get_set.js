var getModels = function (path, f) {
    $.get("/admin/api/models?p_path=" + path, f);
};
var sortNodes = function (id, items, f) {
    var items_str = "";
    $(items).each(function (i) {
        items_str += "items=" + this + "&";
    });
    items_str = items_str.substring(0, items_str.length - 1);
    $.ajax({
        url: "/admin/api/sort?parentId=" + id,
        data: items_str,
        traditional: true,
        success: f,
        type: "POST",
        datatype: "json"
    });
}
var getSearchTypes = function (root,f) {
    $.get("/admin/api/searchtypes?root=" + root, function (d) {
        f(d);
    });
}
var getMarkup = function (parentId, type, variant, f, fromVariant, contentId) {
    $.get("/admin/api/edit?" + (parentId == null ? "" : "parentId=" + parentId + "&")
        + (contentId == null ? "" : "contentId=" + contentId + "&")
        + "&p_variant=" + variant + "&p_type=" + type + /*"&p_path=" + path +*/ "&p_fromVariant=" + (fromVariant == undefined ? "" : fromVariant), f, "html");
}
var getPrepopulatedMarkup = function (type,f) {
    $.get("/admin/api/prepopulatedEdit?p_type="+type, f, "html");
}
var getCreateDialog = function (f, t) {
    $.get("/admin/api/createdialog" + (t === undefined ? "" : "?type=" + t), f, "html");
}
var getTemplateCreateDialog = function (f, p) {
    $.get("/admin/task/createtemplate" + (p === undefined ? "" : "?path=" + p), f, "html");
}
var getTemplateFolderCreateDialog = function (f, p) {
    $.get("/admin/task/createfolder" + (p === undefined ? "" : "?path=" + p), f, "html");
}
var getSettings = function (f) {
    $.get("/admin/settings/edit", f, "html");
}
var getVariants = function (f) {
    $.get("/admin/api/variants", f);
}
var getVariantsForPath = function (path,f) {
    $.get("/admin/api/variantsfornode/?path="+path, f);
}
var getVariantsForId = function (id, f) {
    $.get("/admin/api/variantsfornodebyid/?id=" + id, f);
}
var setDeleteTemplate = function (p, f) {
    $.post("/admin/task/deletetemplate?path=" + p, f);
}
var setDeleteTemplateFolder = function (p, f) {
    $.post("/admin/task/deletetemplatefolder?path=" + p, f);
}
var setPublish = function (id, variant, descendants, f) {
    var path = "/admin/api/publish?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    if (descendants != undefined)
        path += "&descendants=" + descendants;
    $.get(path, f);
}
var setUnpublish = function (id, variant, descendants, f) {
    var path = "/admin/api/unpublish?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    if (descendants != undefined)
        path += "&descendants=" + descendants;
    $.get(path, f);
}
var setDelete = function (id, f, variant) {
    var path = "/admin/api/delete?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    $.get(path, f);
}
var setMoveTemplate = function (from, to, f) {
    var path = "/admin/task/movetemplate?from=" + from + "&to=" + to;
    $.post(path, f);
}
var setMoveTemplateFolder = function (from, to, f) {
    var path = "/admin/task/movetemplatefolder?from=" + from + "&to=" + to;
    $.post(path, f);
}
var setMove = function (from, to, f) {
    var path = "/admin/api/move?startId=" + from + "&destinationId=" + to;
    $.get(path, f);
}
var setCopy = function (id, parentId, includeDescendants, f) {
    var path = "/admin/api/copy?id=" + id + "&parentId=" + parentId + "&includeDescendants=" + includeDescendants;
    $.get(path, f);
}
var setDeleteRevision = function (id, f) {
    var path = "/admin/api/deleterevision?id=" + id;
    $.get(path, f);
}
var setRevert = function (id, f) {
    var path = "/admin/api/revert?id=" + id;
    $.get(path, f);
}
var getFieldGroups = function (t, f) {
    $.get("/admin/api/fieldgroups?type=" + t, f);
}
var getNotifyDialog = function (p, f) {
    $.get("/admin/api/NotifyDialog?p_path=" + p, f);
}
var getLocalisationDialog = function (p, f) {
    $.get("/admin/api/LocalisationDialog?p_path=" + p, f);
}
var getDomainMappingDialog = function (p, f) {
    $.get("/admin/api/DomainMappingDialog?p_path=" + p, f);
}
var getTasks = function (f) {
    $.get("/admin/task/index", f);
}
var getTaskCreateDialog = function (f) {
    $.get("/admin/task/CreateTaskDialog", f);
}
var getTaskMarkup = function (f, type, id) {
    var type = isNullEmpty(type) ? "" : ("type=" + type);
    var id = isNullEmpty(id) ? "" : "id=" + id;
    if (!isNullEmpty(type))
        type += "&"
    $.get("/admin/task/Edit?" + type + id, f);
}
var getUsers = function (f) {
    $.get("/admin/admin/index", f, "html");
}
var getUserMarkup = function (u, f) {
    $.get("/admin/admin/edit?username=" + u, f, "html");
}
var setDeleteUser = function (u, f) {
    $.get("/admin/admin/delete?username=" + u, f);
}
var getRevisions = function (id, variant, f) {
    $.get("/admin/api/revisions?id=" + id + "&variant=" + variant, f, "html");
}
var getCompareMarkup = function (id, f) {
    $.get("/admin/api/compare?id=" + id, f, "html");
}
var getCacheInfo = function (path, f) {
    $.get("/admin/api/cacheinfo?p_path=" + path, f);
}
var getUserRoles = function (f) {
    $.get("/admin/api/userroles", f);
}
var getUserLanguage = function (f) {
    $.get("/admin/api/userlanguage", f);
}
var setCacheInfo = function (path, value, f) {
    $.post("/admin/api/cacheinfo?p_path=" + path + "&value=" + value, f);
}
var deleteParameters = function (key, f) {
    $.get("/admin/settings/DeleteParameters?key=" + key, function (data) {
        if (data.success) {
            f();
        } else {
            msg(false, data.message);
        }
    });
}
var getEditorParametersMarkup = function (f, settingsType, modelType, propertyName) {
    $.get("/admin/settings/EditParameters?settingsType=" + settingsType + "&modelType=" + modelType + "&propertyName=" + propertyName, f);
}
var getContent = function (path, f) {
    $.get("/admin/api/content?path=" + path, f);
}
var getContentByParentId = function (parentId, f) {
    $.get("/admin/api/contentbyparentid?parentid=" + parentId, f);
}

var getTemplates = function (path, f) {
    $.get("/admin/task/templates?path=" + path, f);
}
var getPath = function (id, f) {
    $.get("/admin/api/getpath?id=" + id, f);
};
var getStartPath = function (f) {
    $.get("/admin/api/startpath", f);
};
var getStartId = function (f) {
    $.get("/admin/api/startid", f);
};
var getSearch = function (term, f, type, root) {
    $.get("/admin/api/search?q=" + term + "&type=" + type + "&root=" + root, f, "html");
}
var setRepublishEntireSite = function (f) {
    $.post("/admin/api/RepublishEntireSite", f);
}
var getRepublishEntireSiteStatus = function (f) {
    $.get("/admin/api/GetRepublishEntireSiteStatus", f);
}
