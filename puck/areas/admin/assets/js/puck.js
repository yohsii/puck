var obj = {};
//$(document).ready(function () {
var ctop = $(".menutop");
var cleft = $(".leftarea");
var cright = $(".rightarea .content");
var cmsg = $(".rightarea .message");
var getModels = function (path, f) {
    $.get("/admin/api/models?p_path=" + path, f);
};
var sortNodes = function (path, items, f) {
    var items_str = "";
    $(items).each(function (i) {
        items_str += "items=" + this + "&";
    });
    items_str=items_str.substring(0, items_str.length - 1);
    $.ajax({
        url: "/admin/api/sort?p_path=" + path,
        data: items_str,
        traditional: true,
        success: f,
        type: "POST",
        datatype: "json"
    });
}
var getMarkup = function (path, type, variant, f, fromVariant) {
    $.get("/admin/api/edit?variant=" + variant + "&type=" + type + "&p_path=" + path+"&fromVariant="+(fromVariant==undefined?"":fromVariant), f,"html");
}
var getCreateDialog = function (f, t) {
    $.get("/admin/api/createdialog" + (t === undefined ? "" : "?type=" + t), f, "html");
}
var getSettings = function (f) {
    $.get("/admin/settings/edit", f, "html");
}
var getVariants = function (f) {
    $.get("/admin/api/variants", f);
}
var setPublish = function (id,variant,descendants, f) {
    var path = "/admin/api/publish?id=" + id;
    if (variant != undefined)
        path += "&variant=" + variant;
    if (descendants != undefined)
        path += "&descendants=" + descendants;
    $.get(path, f);
}
var setUnpublish = function (id,variant,descendants, f) {
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
        path += "&variant="+variant;
    $.get(path, f);
}
var setMove = function (from, to, f) {
    var path = "/admin/api/move?start=" + from+"&destination="+to;
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
var getFieldGroups = function (t,f) {
    $.get("/admin/api/fieldgroups?type="+t, f);
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
var getTaskMarkup = function (f,type,id) {
    var type = isNullEmpty(type) ? "" : ("type=" + type);
    var id = isNullEmpty(id) ? "" : "id=" + id;
    if (!isNullEmpty(type))
        type+="&"
    $.get("/admin/task/Edit?"+type+id, f);
}
var getUsers = function (f) {
    $.get("/admin/admin/index", f, "html");
}
var getUserMarkup = function (u,f) {
    $.get("/admin/admin/edit?username="+u, f, "html");
}
var setDeleteUser = function (u,f) {
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
};
var getPath = function (id, f) {
    $.get("/admin/api/getpath?id=" + id, f);
};
var getStartPath = function (f) {
    $.get("/admin/api/startpath", f);
};
var getSearch = function (term,f,type,root) {
    $.get("/admin/api/search?q="+term+"&type="+type+"&root="+root, f,"html");
}
var showSearch = function (term,type,root) {
    getSearch(term, function (d) {
        if (canChangeMainContent()) {
            cright.html(d);
            afterDom();
        }
    },type,root);
}
var showUserMarkup = function (username) {
    getUserMarkup(username, function (d) {
        overlay(d, 580);
        wireForm($(".overlayinner form"), function (data) {
            showUsers();
            if ($(".overlayinner input[name=UserName]").val() == userName) {
                userRoles = $(".overlayinner select[name=Roles]").val();
                hideTopNav();
                getUserLanguage(function (d) { defaultLanguage = d; });
                startPath = data.startPath;
            }
            overlayClose();
        }, function (data) {
            $(".overlayinner .msg").show().html(data.message);            
        });
    });
}
var showUsers = function () {
    getUsers(function (data) {
        if (!canChangeMainContent())
            return;
        cright.html(data);
        cright.find(".create").click(function (e) {
            e.preventDefault();
            showUserMarkup("");
        });
        cright.find(".edit").click(function (e) {
            e.preventDefault();
            var name = $(this).attr("data-username");
            showUserMarkup(name);
        });
        cright.find(".delete").click(function (e) {
            e.preventDefault();
            if (!confirm("sure?"))
                return;
            var el = $(this);
            var name = el.attr("data-username");
            setDeleteUser(name, function (d) {
                if (d.success) {
                    el.parents("tr:first").remove();
                } else {
                    msg(false, d.message);
                }
            });
        });
    });
}
var revisionsFor = function (vcsv, id) {
    var variants = vcsv.split(",");
    if (variants.length == 1) {
        showRevisions(variants[0], id);
    } else {
        var markup = $(".interfaces .revision_for_dialog").clone();
        markup.find(".descendantscontainer").hide();
        for (var i = 0; i < variants.length; i++) {
            markup.find("select").append(
                "<option value='" + variants[i] + "'>" + variants[i] + "</option>"
            );
        }
        overlay(markup, 400, 150);
        markup.find("button").click(function (e) {
            e.preventDefault();
            var variant = markup.find("select").val();
            showRevisions(variant, id);
            overlayClose();
        });
    }
}
var showRevisions = function (variant, id) {
    getRevisions(id, variant, function (data) {
        if (!canChangeMainContent())
            return;
        cright.html(data);
        cright.find(".compare").click(function (e) {
            e.preventDefault();
            var el = $(this);
            showCompare(el.attr("data-id"));
        });
        cright.find(".delete").click(function (e) {
            e.preventDefault();
            if (!confirm("sure?"))
                return;
            var el = $(this);
            setDeleteRevision(el.attr("data-id"), function (data) {
                if (data.success == true) {
                    el.parents("tr:first").remove();
                } else {
                    msg(false, data.message);
                }
            });
        });
        cright.find(".revert").click(function (e) {
            e.preventDefault();
            if (!confirm("sure?"))
                return;
            var el = $(this);
            setRevert(el.attr("data-id"), function (data) {
                if (data.success) {
                    displayMarkup(data.path, data.type, data.variant);
                } else {
                    msg(false, data.message);
                }
            });
        });
    });
}
var showCompare = function (id) {
    getCompareMarkup(id, function (data) {
        overlay(data);
        $(".overlayinner").find("button.revert").click(function (e) {
            e.preventDefault();
            if (!confirm("sure?"))
                return;
            var el = $(this);
            setRevert(el.attr("data-id"), function (d) {
                if (d.success) {
                    overlayClose();
                    displayMarkup(d.path, d.type, d.variant);
                } else {
                    overlayClose();
                    msg(false, d.message);
                }
            });
        });
        var displays = $(".overlayinner .grid_5>.fields");
        var first = displays.first();
        var second = displays.last();
        first.find(".fieldwrapper:not(.complex)").each(function (i) {
            var el = $(this);
            var propName = el.attr("data-fieldname");
            var el2 = second.find(".fieldwrapper[data-fieldname='" + propName + "']");
            var elval = el.find(".editor-field").addClass("compared");
            var el2val = el2.find(".editor-field").addClass("compared");
            if (el2.length == 0 || elval.html() != el2val.html()) {
                elval.css({ backgroundColor: "#ffeeee" });
                el2val.css({ backgroundColor: "#ffeeee" });
            } else {
                elval.css({ backgroundColor: "#eeffee" });
                el2val.css({ backgroundColor: "#eeffee" });
            }
        });
        second.find(".fieldwrapper:not(.complex) .editor-field:not(.compared)").css({ backgroundColor: "#ffeeee" });
    });
}
var showCacheInfo = function (path) {
    getCacheInfo(path, function (data) {
        if (data.success) {
            var markup = $(".main.grid .interfaces .cache_exclude_dialog").clone();
            overlay(markup, 400, 150);
            if (data.result) {
                markup.find("input").attr("checked", "checked");
            }
            $(".overlayinner").find("button").click(function (e) {
                setCacheInfo(path, markup.find("input").is(":checked"), function (data) {
                    if (data.success) {
                        overlayClose();
                    } else {
                        msg(false, data.message);
                        overlayClose()
                    }
                });
            });
        } else {
            msg(false, data.message);
        }
    });
}
var editParameters = function (settingsType, modelType, propertyName, success) {
    getEditorParametersMarkup(function (data) {
        overlay(data, 500);
        var form = $(".overlayinner form");
        wireForm(form, function (data) {
            msg(true, "parameters updated");
            success();
            overlayClose();
        }, function (data) {
            msg(false, data.message);
        });
    }, settingsType, modelType, propertyName);
}
var showTasks = function () {
    if (!canChangeMainContent())
        return false;
    getTasks(function (data) {
        cright.html(data);
        cright.find("a").click(function (e) {
            e.preventDefault();
            var el = $(this);
            if (el.hasClass("create_task")) {
                createTask();
                return;
            }
            if (el.hasClass("delete")) {
                if (!confirm("sure?"))
                    return;
                $.get(el.attr("href"), function (d) {
                    if (d.success) {
                        msg(true, "task deleted");
                        el.parents("tr:first").remove();
                    } else {
                        msg(false, d.message);
                    }
                });
                return;
            }
            $.get(el.attr("href"), function (d) {
                overlay(d, 500);
                var form = $(".overlayinner form");
                wireForm(form, function (data) {
                    msg(true, "task updated");
                    overlayClose();
                    showTasks();
                }, function (data) {
                    msg(false, data.message);
                });
            });
        });
    });
}
var createTask = function () {
    getTaskCreateDialog(function (data) {
        overlay(data, 400, 150);
        $(".overlayinner button").click(function (e) {
            e.preventDefault();
            var typeSelect = $(".overlayinner select[name=type]");
            var type = typeSelect.val();
            getTaskMarkup(function (data) {
                overlayClose();
                overlay(data, 500);
                var form = $(".overlayinner form");
                wireForm(form, function (data) {
                    msg(true, "task updated");
                    overlayClose();
                    showTasks();
                }, function (data) {
                    msg(false, data.message);
                });
            }, type);
        });
    });
}
jQuery.validator.setDefaults({
    ignore:""
});
var checkEnter=function(e) {
    e = e || event;
    var txtArea = /textarea/i.test((e.target || e.srcElement).tagName);
    return txtArea || (e.keyCode || e.which || e.charCode || 0) !== 13;
}
var wireForm = function (form, success, fail) {
    $.validator.unobtrusive.parse(form);
    form.keypress(checkEnter);
    form.submit(function (e) {
        if (tinyMCE != undefined) {
            tinyMCE.triggerSave();
        }
        if (form.valid()) {
            e.preventDefault();            
            var values = form.serialize();
            var fd = new FormData(form.get(0));
            $.ajax({
                url: form.attr("action"),
                data: fd,
                processData: false,
                contentType: false,
                type: 'POST',
                success: function (data) {
                    if (data.success == true) {
                        success(data);
                    } else {
                        fail(data);
                    }
                }
            });
        } else {
            var err_el = cright.find(".input-validation-error:first");
            cright.find("[href='#" + err_el.parents(".tab-pane").attr("id") + "']").click();
            err_el.focus();
        }
    });
}
var newContent = function (path, type) {
    getCreateDialog(function (data) {
        overlay(data, 400, 250, 100);
        $(".overlayinner button").click(function () {
            var type = $(".overlayinner select[name=type]").val();
            var variant = $(".overlayinner select[name=variant]").val();
            overlayClose()
            displayMarkup(path, type, variant);
        });
    }, type);
}
var getDrawContent = function (path, el, sortable,f) {
    if (el == undefined) {
        var nodeParent = dirOfPath(path);
        el = cleft.find(".node[data-children_path='" + nodeParent + "']");
    }
    getContent(path, function (data) {
        for (var k in data.published) {
            publishedContent[k] = data.published[k];
        }
        for (var i = 0; i < data.children.length; i++) {
            haveChildren[data.children[i]] = true;
        }
        draw(data.current, el, sortable);
        el.find(".node").each(function () {
            var n = $(this);
            if (!haveChildren[n.attr("data-path")])
                n.find(".expand").css({visibility:"hidden"});
        });
        if(f!=undefined)
            f();
    });
}
var draw = function (data, el, sortable) {
    var str = "";
    var toAppend = $("<ul/>");
    for (var p in data) {//paths as keys
        dbcontent[p] = data[p];
        var variants = [];
        var hasUnpublished = false;
        for (var v in data[p]) {//variant as keys
            variants.push(v);
            if (data[p][v].Published==false) {
                hasUnpublished = true;
            }
        }
        var node;
        if (!!data[p][defaultLanguage])
            node = data[p][defaultLanguage];
        else
            node = data[p][v];
        var elnode = $("<li/>").addClass("node");
        var elinner = $("<div class='inner'/>");
        elnode.append(elinner);
        if (hasUnpublished)
            elnode.addClass("unpublished");
        elinner.append($("<i class=\"puck_icon\"></i>"))
        elinner.append($("<i class=\"icon-chevron-right expand\"></i>"))
        elinner.append($("<i class=\"icon-cog menu\"></i>"))
                .append("<span class='nodename'>" + node.NodeName + "&nbsp;" + "</span>");
        for (var i = 0; i < variants.length; i++) {
            var vel = $("<span class=\"variant\"/>").attr("data-variant", variants[i]).html(variants[i] + "&nbsp;");
            if (publishedContent[node.Path] != undefined && publishedContent[node.Path][variants[i]] != undefined) {
                vel.addClass("published");
            }
            elinner.append(vel);
        }
        elnode.attr({
            "data-type_chain": typeFromChain(node.TypeChain)
            , "data-type": node.Type
            , "data-id": node.Id
            , "data-path": node.Path
            , "data-nodename": node.NodeName
            , "data-variants": variants.join(",")
            , "data-parent_path": dirOfPath(node.Path)
            , "data-children_path": node.Path + "/"
            , "data-published": node.Published
        });
        toAppend.append(elnode);        
    }
    el.find("ul").remove();
    el.append(toAppend);
    if (sortable) {
        toAppend.sortable({
            cursorAt:{top:0,left:0},
            update: function (event, ui) {
                var parent = ui.item.parents("li[data-children_path]:first");
                var sortPath = parent.attr("data-children_path");
                var items = [];
                parent.find("li.node").each(function () {
                    items.push($(this).attr("data-path"));
                });
                sortNodes(sortPath, items, function () { });
            }
        });
    }
}
var displayMarkup = function (path, type, variant,fromVariant) {
    getMarkup(path, type, variant, function (data) {
        cright.hide().html(data);
        var translations = $("<ul/>").addClass("translations");
        var node = cleft.find(".node[data-path='" + path + "']");
        if (node.length > 0) {
            var dataTranslations = node.attr("data-variants").split(',');
            if (dataTranslations.length > 1) {
                for (var i = 0; i < dataTranslations.length; i++) {
                    (function () {
                        var dataTranslation = dataTranslations[i];
                        var published = true;

                        if (dbcontent[path][dataTranslation].Published == false) {
                            published = false;
                        }
                        
                        var dtli = $("<li/>");
                        if (!published)
                            dtli.addClass("unpublished");
                        if (dataTranslation != variant) {
                            var lnk = $("<a href='#'/>").html("-" + variantNames[dataTranslation]);
                            lnk.click(function (e) {
                                e.preventDefault();
                                var vcode = dataTranslation;
                                displayMarkup(path, type, vcode);
                            });
                            dtli.append(lnk)
                        } else {
                            dtli.append("-" + variantNames[dataTranslation]);
                        }
                        translations.append(dtli);
                    })();
                }
                cright.prepend(translations);
            }
        }
        //get field groups and build tabs
        getFieldGroups(type, function (data) {
            var groups = [];
            $(data).each(function (i) {
                var val = this
                if (!groups.contains(val.split(':')[1]))
                    groups.push(val.split(":")[1]);
            });
            var tabHtml = '<ul class="nav nav-tabs">';
            $(groups).each(function (i) {
                var val = this;
                tabHtml += '<li class="' + (i == 0 ? "active" : "") + '"><a class="fieldtabs" href="#fieldtabs' + i + '">' + val + '</a></li>';
            });
            tabHtml += '<li class=""><a class="fieldtabs" href="#fieldtabs' + groups.length + '">default</a></li>';
            tabHtml += '</ul>';

            tabHtml += '<div class="tab-content">';
            $(groups).each(function (i) {
                var val = this;
                tabHtml += '<div data-group="' + val + '" class="tab-pane ' + (i == 0 ? "active" : "") + '" id="fieldtabs' + i + '">&nbsp;</div>';
            });
            tabHtml += '<div data-group="default" class="tab-pane" id="fieldtabs' + groups.length + '">&nbsp;</div>';
            tabHtml += "</div>";
            cright.find("form").prepend(tabHtml);
            cright.find(".nav .fieldtabs").click(function (e) {
                e.preventDefault();
                $(this).tab("show");
            });
            $(data).each(function (i) {
                var val = this;
                var type = val.split(":")[0];
                var group = val.split(":")[1];
                var field = val.split(":")[2];
                var fieldWrapper = $(".fieldwrapper[data-fieldname='" + field + "']");
                var groupContainer = $(".tab-pane[data-group='" + group + "']");
                groupContainer.append(fieldWrapper);
            });
            cright.find("div.fields>.fieldwrapper").each(function () {
                var el = $(this);
                var fieldname = el.attr("data-fieldname");
                if (fieldname.split(".").length > 1)
                    cright.find(".fieldwrapper[data-fieldname='" + fieldname.split(".").slice(0, -1).join(".") + "']").append(el);
                else el.appendTo(cright.find("[data-group='default']"));
            })
            afterDom();
            cright.show();
            cright.find(".fieldtabs:first").click();
            setChangeTracker();
            highlightSelectedNode(path);
        });
        //publish btn
        if (userRoles.contains("_publish")) {
            cright.find(".content_publish").click(function () {
                cright.find("input:hidden[name='Published']").val("true");                
            });
        } else { cright.find(".content_publish").hide();}
        //udpate btn
        cright.find(".content_update").click(function () {
            cright.find("input:hidden[name='Published']").val("false");
        });
        //preview btn
        if (path.slice(-1) != '/') {
            cright.find(".content_preview").click(function (e) {
                e.preventDefault();
                window.open("/admin/api/preview?path=" + path +"&variant=" + variant, "_blank");
            });
        } else { cright.find(".content_preview").hide();}

        wireForm(cright.find('form'), function (data) {
            msg(true, "content updated");
            getDrawContent(dirOfPath(path), undefined, true, function () {
                displayMarkup(data.path, type, variant);
            });            
        }, function (data) {
            msg(false, data.message);
        });        
    },fromVariant);
}
var setChangeTracker = function () {
    changed = false;
    cright.find(":input").change(function (e) {
        changed = true;
    });
}
var msg = function (success, str) {
    var btnClass = "";
    if (success === false) { btnClass = "btn-danger"; }
    else if (success === true) { btnClass = "btn-success"; }
    var el = $("<div style='display:none;' class='btn " + btnClass + "'>" + str + "</div>");
    var remove = $("<div class='btn btnclose'>x</div>").click(function () { $(this).parent().remove(); });
    el.append(remove);
    cmsg.html(el);
    el.fadeIn();
}
var puckmaxwidth = 960;
var overlayClose = function () {
    $(".overlayinner,.overlay").remove();
    $("body").css({ overflow: "initial" });
    $(document).unbind("keyup");
}
var overlay = function (el, width, height, top) {
    var ov = $("<div class='overlay'/>");
    var inner = $("<div class='overlayinner container_12'/>");
    var clear = $("<div class='clearboth'/>");
    if (!!width)
        inner.css({ width: width + "px" });
    if (!!height)
        inner.css({ height: height + "px" });
    if (!!top)
        inner.css({ top: top + "px" });
    var close = $("<div class='btn btn-link'><i class='icon-remove-sign'/>&nbsp;close</div>");
    close.click(function () {
        overlayClose();
    });
    inner.append(close).append(clear).append(el);
    cright.append(ov).append(inner);
    height = height || $(window).height() * 0.8;
    if (height)
        inner.css({ height: height + "px" });
    inner.css({ left: ($(window).width() - (width || $(".overlayinner").width())) / 2 + "px" });
    if (!top)
        inner.css({ top: ($(window).height() - height) / 2 + "px" });
    $("body").css({ overflow: "hidden" });
    afterDom();
    $(document).keyup(function(e) {
        if (e.keyCode == 27) { overlayClose(); }
    });
}
var afterDomActions = [];
var onAfterDom = function (f) {
    afterDomActions.push(f);
}
var afterDom = function () {
    while (afterDomActions.length) {
        afterDomActions.pop()();
    }
}
var changed = false;
var canChangeMainContent=function(){
    if(cright.find(".fieldwrapper").length> 0 && changed)
        if (confirm("sure you want to move away from this page?")) {
            changed = false;
            return true;
        } else {
            return false;
        }
        else
            return true;
}
var hideTopNav = function () {
    $(".menutop a[data-permission]").each(function () {
        var el =$(this);
        var perm = el.attr("data-permission");
        if (userRoles.contains(perm))
            el.show();
        else
            el.hide();
    });
    if (userRoles.contains("_create")) {
        $(".create_default").show();
    } else {
        $(".create_default").hide();
    }
}
$('a.settings').click(function (e) {
    e.preventDefault();
    if (!canChangeMainContent())
        return false;
    getSettings(function (data) {
        cright.html(data);
        afterDom();
        //setup validation
        wireForm(cright.find('form'), function (data) {
            msg(true, "settings updated.");
            window.scrollTo(0);
            getVariants(function (data) {
                languages = data;
            });
        }, function (data) {
            msg(false, data.message);
        });
        setChangeTracker();
    });
});
var dirOfPath = function (s) {
    if (s[s.length - 1] == "/")
        return s;
    return s.substring(0, s.lastIndexOf("/") + 1);
}
var isRootItem = function (s) {
    var matches = s.match(/\//g);
    if (matches==null)
        throw "isRootItem - invalid input: " + s;
    return matches.length == 1;
}
var typeFromChain = function (s) {
    return s.split(" ")[0];
}
var untranslated = function (variants) {
    var untranslated = [];
    $(languages).each(function () {
        if (!variants.split(",").contains(this.Key))
            untranslated.push(this.Key);
    });
    return untranslated.length == 0 ? false : untranslated;
}
//bindings
$("button.search").click(function () {
    $("input.search").animate({ width: 100, opacity: 1 }, 500);
    $(".search_options").animate({ opacity: 1 }, 500);
});
$("input.search").keypress(function (e) {
    e = e || event;
    if ((e.keyCode || e.which || e.charCode || 0) === 13) {
        var term = $(this).val();
        showSearch(term,searchType,searchRoot);
    };
});
    //root new content button
    $(".create_default").show().click(function () { newContent("/"); });
    //task list
    $(".menutop .tasks").click(function (e) { e.preventDefault(); showTasks(); });
    //users
    $(".menutop .users").click(function (e) { e.preventDefault(); showUsers(); });
    //content tree expand
    $("body").on("click", "ul.content li.node i.expand", function () {
        //get children content
        var node = $(this).parents(".node:first");
        var descendants = node.find("ul");
        if (descendants.length > 0) {//show
            if (descendants.first().is(":hidden")) {
                node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
                descendants.show();
            } else {//hide
                node.find("i.expand").removeClass("icon-chevron-down").addClass("icon-chevron-right");
                descendants.hide();
            }
        } else {
            getDrawContent(node.attr("data-children_path"),node,true);
            node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
        }
    });
    //node settings dropdown
    cleft.find("ul.content").on("click", "li.node i.menu", function (e) {
        //display dropdown
        var node = $(this).parents(".node:first");
        var left = node.offset().left;
        var top = node.offset().top + 30;
        var dropdown = $(".node-dropdown");
        dropdown
            .addClass("open")
            .css({ top: top + "px", left: left + "px" })
            .attr("data-context", node.attr("data-id"));
        e.stopPropagation();
        $("html").on("click", "", function () {
            dropdown.removeClass("open");
            $("html").off();
        });
        //filter menu items according to context -- ie COULD this option be shown in the current context
        //filter translation item
        var totranslate = untranslated(node.attr("data-variants"));
        if (totranslate)
            dropdown.find("a[data-action='translate']").parents("li").show();
        else
            dropdown.find("a[data-action='translate']").parents("li").hide();
        //filter publish/unpublish
        if (publishedVariants(node.attr("data-path")) !=false)
            dropdown.find("a[data-action='unpublish']").parents("li").show();
        else
            dropdown.find("a[data-action='unpublish']").parents("li").hide();
    
        if (unpublishedVariants(node.attr("data-path")) !=false)    
            dropdown.find("a[data-action='publish']").parents("li").show();
        else
            dropdown.find("a[data-action='publish']").parents("li").hide();
        //filter domain
        if (isRootItem(node.attr("data-path"))) {
            dropdown.find("a[data-action='domain']").parents("li").show();
        } else {
            dropdown.find("a[data-action='domain']").parents("li").hide();
        }
        //filter move - disallow root move
        if (node.attr("data-path").split('/').length - 1 == 1)
            dropdown.find("a[data-action='move']").parents("li").hide();
        else
            dropdown.find("a[data-action='move']").parents("li").show();
        //filter menu items according to permissions -- ie can user access option
        dropdown.find("a[data-action]").each(function () {
            var permission = $(this).attr("data-permission");
            if (!userRoles.contains(permission)) $(this).parents("li").hide();        
        });
    });
    var dialogForVariants = function (variants) {
        var dialog = $(".interfaces .revision_for_dialog").clone();
        $.each(variants, function () {
            dialog.find(".variantcontainer select").append("<option value='" + this + "'>" + this + "</option>");
        });
        $.each(languages, function () {
            dialog.find(".descendantscontainer select").append("<option value='" + this.Key + "'>" + this.Key + "</option>");        
        });
        dialog.find(".descendantscontainer select").prepend("<option selected value=''>None</option>");
        return dialog;
    }
    var unpublishedVariants = function (path) {
        var variants = [];
        cleft.find(".node[data-path='" + path + "']>.inner>.variant").each(function () {
            if (!$(this).hasClass("published"))
                variants.push($(this).attr("data-variant"));
        });
        return variants.length == 0 ? false : variants;
    }
    var publishedVariants = function (path) {
        var variants = [];
        cleft.find(".node[data-path='" + path + "']>.inner>.variant").each(function () {
            if ($(this).hasClass("published"))
                variants.push($(this).attr("data-variant"));
        });
        return variants.length == 0 ? false : variants;
    }
    //menu items
    $(".node-dropdown a").click(function () {
        var el = $(this);
        var action = el.attr("data-action");
        var context = el.parents(".node-dropdown").attr("data-context");
        var node = $(".node[data-id='" + context + "']");
        switch (action) {
            case "delete":
                if (confirm("sure?")) {
                    var doDelete = function (id, variant) {
                        setDelete(id, function (data) {
                            if (data.success === true) {
                                if (variant == "" || variant == undefined) {
                                    node.remove();
                                } else {
                                    if (node.find("span.variant").length > 1)
                                        node.find("span.variant[data-variant='" + variant + "']").remove();
                                    //else
                                    //node.remove();
                                }
                                getDrawContent(dirOfPath(node.attr("data-path")), undefined, undefined, function () {
                                    highlightSelectedNode(node.attr("data-path"));
                                });
                                overlayClose();
                            } else {
                                msg(false, data.message);
                                overlayClose();
                            }
                        }, variant);
                    }
                    var variants = node.attr("data-variants").split(",");
                    if (variants.length > 1) {
                        var dialog = dialogForVariants(variants);
                        overlay(dialog, 400, 150);
                        dialog.find(".descendantscontainer").hide();
                        dialog.find("button").click(function () {
                            doDelete(node.attr("data-id"), dialog.find("select").val());
                        });
                    } else {
                        doDelete(node.attr("data-id"));
                    }
                }; break;
            case "publish":
                var doPublish = function (id, variant,descendants) {
                    setPublish(id,variant,descendants, function (data) {
                        if (data.success === true) {
                            getDrawContent(dirOfPath(node.attr("data-path")), undefined, true);
                            overlayClose();
                        } else {
                            msg(false, data.message);
                            overlayClose();
                        }
                    });
                }
                var variants = unpublishedVariants(node.attr("data-path"));
                if (variants.length > 1 || 1 == 1) {
                    var dialog = dialogForVariants(variants);
                    dialog.find(".descendantscontainer label").html("Publish descendants?");
                    overlay(dialog, 400, 250);
                    dialog.find("button").click(function () {
                        doPublish(node.attr("data-id"), dialog.find("select[name=variant]").val(), (dialog.find("select[name=descendants]").val() || []).join(','));
                    });
                } else {
                    doPublish(node.attr("data-id"), variants[0]);
                }
                break;
            case "unpublish":
                var doUnpublish = function (id, variant,descendants) {
                    setUnpublish(id,variant,descendants, function (data) {
                        if (data.success === true) {
                            getDrawContent(dirOfPath(node.attr("data-path")), undefined, true);
                            overlayClose();
                        } else {
                            msg(false, data.message);
                            overlayClose();
                        }
                    });
                }
                var variants = publishedVariants(node.attr("data-path"));
                if (variants.length > 1 || 1 == 1) {
                    var dialog = dialogForVariants(variants);
                    dialog.find(".descendantscontainer label").html("Unpublish descendants?");
                    overlay(dialog, 400, 250);
                    dialog.find("button").click(function () {
                        doUnpublish(node.attr("data-id"), dialog.find("select[name='variant']").val(), (dialog.find("select[name='descendants']").val() || []).join(','));
                    });
                } else {
                    doUnpublish(node.attr("data-id"), variants[0]);
                }
                break;
            case "revert":
                revisionsFor(node.attr("data-variants"),node.attr("data-id"));
                break;
            case "cache":
                showCacheInfo(node.attr("data-path"));
                break;
            case "create":
                newContent(node.attr("data-children_path"), node.attr("data-type"));
                break;
            case "move":
                var markup = $(".interfaces .tree_container").clone();
                var el = markup.find(".node:first");
                overlay(markup);
                $(".overlayinner .msg").html("select new parent node for content <b>"+node.attr("data-nodename")+"</b>");
                getDrawContent(startPath, el);
                markup.on("click", ".node span", function (e) {
                    var dest_node = $(this).parents(".node:first");
                    var from = node.attr("data-path");
                    var to = dest_node.attr("data-path");
                    if (!confirm("move " + from + " to " + to+" ?")) {
                        return;
                    }
                    setMove(from, to, function (d) {
                        if (d.success) {
                            if (from.length > to.length)
                                getDrawContent(to + "/");
                            else
                                getDrawContent(dirOfPath(from));
                        } else {
                            msg(false, d.message);
                        }
                        overlayClose();
                    });
                });
                break;
            case "translate":
                getCreateDialog(function (data) {
                    overlay(data, 400, 250);
                    var type = $(".overlayinner select[name=type]");
                    var variant = $(".overlayinner select[name=variant]");
                    var fromVariant = variant.clone().attr("name","fromVariant");
                    $(".overlayinner .typecontainer label").html("Translate from version").siblings().hide().after(fromVariant);
                    type.val(node.attr("data-type"));
                    var variants = node.attr("data-variants").split(",");
                    if (variants.length == 1) {
                        $(".overlayinner .typecontainer").hide();
                        $(".overlayinner").css({height:"170px"});
                    }
                    fromVariant.find("option").each(function () {
                        var option = $(this);
                        var contains = false;
                        $(variants).each(function () {
                            if (this == option.val())
                                contains = true;
                        });
                        if (!contains) {
                            option.remove();
                        }
                    });
                    variant.find("option").each(function () {
                        //check value doesn't exist in variant list
                        var option = $(this);
                        var contains = false;
                        $(variants).each(function () {
                            if (this == option.val())
                                contains = true;
                        });
                        if (contains)
                            option.remove();
                    });
                    $(".overlayinner button").click(function () {
                        displayMarkup(node.attr("data-path"), node.attr("data-type"), variant.val(),fromVariant.val());
                        overlayClose();
                    });
                }, node.attr("data-type"));
                break;
            case "localisation":
                setLocalisation(node.attr("data-path"));
                break;
            case "domain":
                setDomainMapping(node.attr("data-path"));
                break;
        }
    });

    var setLocalisation = function (p) {
        getLocalisationDialog(p, function (data) {
            overlay(data,400,250);
            var form = $('.overlayinner form');
            wireForm(form, function (data) {
                overlayClose();
            }, function (data) {
                msg(false,data.message);
            });
        });
    }
    var setDomainMapping = function (p) {
        getDomainMappingDialog(p, function (data) {
            overlay(data,500,250);
            var form = $('.overlayinner form');
            wireForm(form, function (data) {
                overlayClose();
            }, function (data) {
                msg(false, data.message);
                overlayClose();
            });
        });
    }
    var highlightSelectedNode = function (path) {
        cleft.find(".node").removeClass("selected");
        cleft.find(".node[data-path='"+path+"']").addClass("selected");
    }

    cleft.find("ul.content").on("click", "li.node span.nodename", function () {
        //get markup
        if (!canChangeMainContent())
            return false;
        var node = $(this).parents(".node:first");
        var firstVariant = node.attr("data-variants").split(",")[0];
        displayMarkup(node.attr("data-path"),node.attr("data-type"),firstVariant);
    });

    cleft.on("click", ".search_options", function () {
        var el = $(".interfaces .search_ops").clone();
        overlay(el, 500, 400);
        el.on("click",".node span",function (e) {
            var node = $(this).parents(".node:first");
            var path = node.attr("data-path");
            var close = $('<i class="icon-remove-sign"></i>');
            var pathspan = $("<span/>").html(path);
            el.find(".pathvalue").html('').append(pathspan).append(close);
            close.click(function () {
                el.find(".pathvalue").html('');
            });
        });
        getDrawContent(startPath, el.find(".node"));
        el.find("button").click(function () {
            searchType = el.find("select").val();
            searchRoot = el.find(".pathvalue span").html()||'';
            overlayClose();
            if (!searchRoot.isEmpty() || !searchType.isEmpty()) {
                $(".search_options i").addClass("active");
            } else {
                $(".search_options i").removeClass("active");
            }
        });
        if (!searchType.isEmpty()) {
            el.find("select option[value='" + searchType + "']").attr("selected", "selected");
        }
        if (!searchRoot.isEmpty()) {
            var close = $('<i class="icon-remove-sign"></i>');
            var pathspan = $("<span/>").html(searchRoot);
            el.find(".pathvalue").html('').append(pathspan).append(close);
            close.click(function () {
                el.find(".pathvalue").html('');
            });
        }
    });

//ini
    var searchType = '';
    var searchRoot = '';
    var publishedContent = [];
    var haveChildren = [];
    var dbcontent = [];
    var defaultLanguage = "en-gb";
    var userRoles = [];
    var languages;
    var startPath;
    getUserLanguage(function (d) { defaultLanguage = d; });
    getUserRoles(function (d) { userRoles = d; hideTopNav(); });
    getVariants(function (data) {
        languages = data;
        if (languages.length == 0) {
            onAfterDom(function () {
                msg(0, "take a moment to setup puck. at the very least, choose your languages!");
            });
            $(".menutop .settings").click();        
        }
    });
    getStartPath(function (d) {
        cleft.find("ul.content li:first").attr("data-children_path",d);
        $(".interfaces .tree_container ul.content .node").attr("data-children_path", startPath);
        startPath = d;
        cleft.find(".startpath").html(d);
        getDrawContent(d, undefined, true);
    });

    //extensions
    $.validator.methods.date = function (value, element) {
        {
            if (value == '' || Globalize.parseDate(value, "dd/MM/yyyy HH:mm:ss")!=null) {
                {
                    return true;
                }
            }
            return false;
        }
    }
    String.prototype.isEmpty = function () {
        return this.replace(/\s/g, "").length == 0;
    }
    var isNullEmpty = function (s) {
        if (s == null || s == undefined) return true;
        return s.replace(/\s/g, "").length == 0;
    }
    var isInt = function (s) {
        return /^\d+$/.test(s);
    }
    Array.prototype.contains=function(v){
        var contains=false;
        for(var i =0; i<this.length;i++)
            if(this[i]==v)
                contains=true;
        return contains;
    }
    //});   