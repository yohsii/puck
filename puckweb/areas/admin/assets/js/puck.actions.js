//$(document).ready(function () {
//ini
var obj = {};
var ctop = $(".menutop");
var cleft = $(".leftarea");
var cright = $(".rightarea .content");
var cmsg = $(".rightarea .message");
var searchType = '';
var searchRoot = '';
var searchTerm = '';
var publishedContent = [];
var haveChildren = [];
var dbcontent = [];
var defaultLanguage = "en-gb";
var userRoles = [];
var languages;
var startPath;
var startId;
var newTemplateFolder = function (p) {
    getTemplateFolderCreateDialog(function (d) {
        overlay(d, 500, 300, undefined, "New Template Folder");
        wireForm($(".overlay_screen form"), function (data) {
            getDrawTemplates(p);
            overlayClose();
        }, function (data) {
            $(".overlay_screen .msg").show().html(data.message);
        });
    }, p);
}
var newTemplate = function (p) {
    getTemplateCreateDialog(function (d) {
        overlay(d, 500, 300, undefined, "New Template");
        wireForm($(".overlay_screen form"), function (data) {
            getDrawTemplates(p, undefined, function () {
                cright.find(".node[data-id='" + data.name + "']").click();
            });
            overlayClose();
        }, function (data) {
            $(".overlay_screen .msg").show().html(data.message);
        });
    }, p);
}
var showSearch = function (term, type, root) {
    getSearch(term, function (d) {
        if (canChangeMainContent()) {
            cright.html(d);
            afterDom();
        }
    }, type, root);
}
var searchDialogClose = function () {
    if ($(".search_ops:visible").length > 0) {
        $(".search_ops:visible").fadeOut(function () { $(this).remove(); });
        return true;
    }
    return false;
}
var searchDialog = function (root, f) {
    overlayClose();
    getSearchTypes(root, function (d) {
        if (searchDialogClose())
            return;

        if (!searchRoot.isEmpty() || !searchType.isEmpty()) {
            $(".search_options i").addClass("active");
        } else {
            $(".search_options i").removeClass("active");
        }

        var el = $(".interfaces .search_ops").clone();
        el.css({ left: cright.offset().left - 20 + "px", width: "0px", top: "90px", height: $(window).height() - 90 + "px" });
        cright.append(el);
        el.animate({ width: "280px" }, 200, function () { if (f) f(); });
        $("input.search").animate({ width: 205, opacity: 1 }, 500);
        var types = el.find("select");
        types.html("<option value=''>None</option>");
        $(d).each(function () {
            types.append(
                "<option value='" + this.Type + "'>" + this.Name + "</option>"
            );
        });

        if (!searchTerm.isEmpty()) {
            el.find("input.search").val(searchTerm);
        }

        if (!searchType.isEmpty()) {
            el.find("select option[value='" + searchType + "']").attr("selected", "selected");
        }

        if (!searchRoot.isEmpty()) {
            var close = $('<i class="icon-remove-sign"></i>');
            var pathspan = $("<span/>").html(searchRoot);
            el.find(".pathvalue").html('').append(pathspan).append(close);
            close.click(function () {
                el.find(".pathvalue").html('');
                searchRoot = "";
            });
        }

        el.on("click", ".node span", function (e) {
            var node = $(this).parents(".node:first");
            var path = node.attr("data-path");
            var close = $('<i class="icon-remove-sign"></i>');
            var pathspan = $("<span/>").html(path);
            searchRoot = path;
            el.find(".pathvalue").html('').append(pathspan).append(close);
            close.click(function () {
                el.find(".pathvalue").html('');
                searchRoot = "";
            });
        });
        getDrawContent(startPath, el.find(".node"));
        el.find("button.btn").click(function () {
            searchType = el.find("select").val();
            searchRoot = el.find(".pathvalue span").html() || '';
            var term = el.find(".search").val();
            searchTerm = term;
            showSearch(term, searchType, searchRoot);
        });
        el.find("input.search").keypress(function (e) {
            searchType = el.find("select").val();
            searchRoot = el.find(".pathvalue span").html() || '';
            var term = $(this).val();
            searchTerm = term;
            e = e || event;
            if ((e.keyCode || e.which || e.charCode || 0) === 13) {
                showSearch(term, searchType, searchRoot);
            };
        });
        cright.find(".search_ops input.search").focus();
    });
}
var showUserMarkup = function (username) {
    getUserMarkup(username, function (d) {
        overlay(d, 580, undefined, undefined, "User");
        wireForm($(".overlay_screen form"), function (data) {
            showUsers();
            if ($(".overlay_screen input[name=UserName]").val() == userName) {
                userRoles = $(".overlay_screen select[name=Roles]").val();
                hideTopNav();
                getUserLanguage(function (d) { defaultLanguage = d; });
                startPath = data.startPath;
            }
            overlayClose();
        }, function (data) {
            $(".overlay_screen .msg").show().html(data.message);
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
                "<option value='" + variants[i] + "'>" + variantNames[variants[i]] + "</option>"
            );
        }
        overlay(markup, 400, 150, undefined, "Revision Language");
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
        overlay(data, undefined, undefined, undefined, "Compare");
        $(".overlay_screen").find("button.revert").click(function (e) {
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
        var displays = $(".overlay_screen .compare_revision>.fields");
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
            overlay(markup, 400, 150, undefined, "Cache");
            if (data.result) {
                markup.find("input").attr("checked", "checked");
            }
            $(".overlay_screen").find("button").click(function (e) {
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
        overlay(data, 500, undefined, undefined, "Edit Parameters");
        var form = $(".overlay_screen form");
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
        afterDom();
        cright.find(".tasklist a").click(function (e) {
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
                overlay(d, 500, undefined, undefined, "Edit Task");
                var form = $(".overlay_screen form");
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
        overlay(data, 400, 150, undefined, "Create Task");
        $(".overlay_screen button").click(function (e) {
            e.preventDefault();
            var typeSelect = $(".overlay_screen select[name=type]");
            var type = typeSelect.val();
            getTaskMarkup(function (data) {
                overlayClose();
                overlay(data, 500, undefined, undefined, "Edit Task");
                var form = $(".overlay_screen form");
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
    ignore: ""
});
var checkEnter = function (e) {
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
var newContent = function (guid, type) {
    getCreateDialog(function (data) {
        overlay(data, 400, 250, 100, "New Content");
        $(".overlay_screen button").click(function () {
            var type = $(".overlay_screen select[name=type]").val();
            var variant = $(".overlay_screen select[name=variant]").val();
            overlayClose()
            displayMarkup(guid,type, variant);
        });
    }, type);
}
var getDrawTemplates = function (path, el, f) {
    if (el == undefined) {
        var nodeParent = dirOfPath(path);
        el = $("ul.templates .node[data-path='" + nodeParent + "']");
    }
    getTemplates(path, function (data) {
        drawTemplates(data, el);
        if (f != undefined)
            f();
    });
}
var deleteTemplate = function (name, path) {
    if (!confirm("delete template - " + name + " ?"))
        return;
    setDeleteTemplate(path, function (d) {
        if (d.success)
            $("ul.templates .node[data-path='" + path + "']").remove();
        else msg(false, d.message);
    });
}
var deleteTemplateFolder = function (name, path) {
    if (!confirm("delete template folder - " + name + " ?"))
        return;
    setDeleteTemplateFolder(path, function (d) {
        if (d.success)
            $("ul.templates .node[data-path='" + path + "']").remove();
        else msg(false, d.message);
    });
}
var drawTemplates = function (data, el, sortable) {
    var str = "";
    var toAppend = $("<ul/>");
    for (var i = 0; i < data.length; i++) {
        var node = data[i];
        var elnode = $("<li/>").addClass("node");
        var elinner = $("<div class='inner'/>");
        elnode.append(elinner);
        elinner.append($("<i class=\"puck_icon\"></i>").addClass(node.Type == "folder" ? "icon-folder-close" : ""))
        elinner.append($("<i class=\"icon-chevron-right expand\"></i>"))
        elinner.append($("<i class=\"icon-cog menu\"></i>"))
                .append("<span class='nodename'>" + node.Name + "&nbsp;" + "</span>");
        elnode.attr({
            "data-type": node.Type
            , "data-path": node.Path
            , "data-id": node.Path
            , "data-name": node.Name
            , "data-has_children": node.HasChildren
        });
        if (!node.HasChildren)
            elnode.find(".expand").css({ visibility: "hidden" });
        toAppend.append(elnode);
    }
    el.find("ul").remove();
    el.append(toAppend);
}

var getDrawContent = function (id, el, sortable, f) {
    if (el == undefined) {
        el = cleft.find(".node[data-id='" + id + "']");
    }
    getContentByParentId(id, function (data) {
        /*var plevel = path.split('/').length - 1;
        for (var k in publishedContent) {
            var level = k.split('/').length - 1;
            if (plevel == level) {
                publishedContent[k] = undefined;
            }
        }*/
        for (var k in data.published) {
            publishedContent[k] = data.published[k];
        }
        for (var i = 0; i < data.children.length; i++) {
            haveChildren[data.children[i]] = true;
        }
        draw(data.current, el, sortable);
        el.find(".node").each(function () {
            var n = $(this);
            if (!haveChildren[n.attr("data-id")])
                n.find(".expand").css({ visibility: "hidden" });
        });
        if (f != undefined)
            f();
    });
}
var draw = function (data, el, sortable) {
    var str = "";
    var toAppend = $("<ul/>");
    for (var p in data) {//ids as keys
        dbcontent[p] = data[p];
        var variants = [];
        var hasUnpublished = false;
        for (var v in data[p]) {//variant as keys
            variants.push(v);
            if (data[p][v].Published == false) {
                hasUnpublished = true;
            }
        }
        var node;
        if (!!data[p][defaultLanguage])
            node = data[p][defaultLanguage];
        else
            node = data[p][variants[0]];
        var elnode = $("<li/>").addClass("node");
        var elinner = $("<div class='inner'/>");
        elnode.append(elinner);
        if (hasUnpublished)
            elnode.addClass("unpublished");
        elinner.append($("<i class=\"icon-search\"></i>"));
        elinner.append($("<i class=\"puck_icon\"></i>"))
        elinner.append($("<i class=\"icon-chevron-right expand\"></i>"))
        elinner.append($("<i class=\"icon-cog menu\"></i>"))
                .append("<span class='nodename'>" + node.NodeName + "&nbsp;" + "</span>");
        for (var i = 0; i < variants.length; i++) {
            var vel = $("<span class=\"variant\"/>").attr("data-variant", variants[i]).html(variants[i] + "&nbsp;");
            if (publishedContent[node.Id] != undefined && publishedContent[node.Id][variants[i]] != undefined) {
                vel.addClass("published");
            }
            elinner.append(vel);
        }
        elnode.attr({
            "data-type_chain": typeFromChain(node.TypeChain)
            , "data-type": node.Type
            , "data-id": node.Id
            , "data-parent_id": node.ParentId
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
            cursorAt: { top: 0, left: 0 },
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
var displayMarkup = function (parentId, type, variant, fromVariant,contentId) {
    getMarkup(parentId, type, variant, function (data) {
        cright.hide().html(data);

        if (!type) {
            type = cright.find("input[name=Type]").val();
        }

        var translations = $("<ul/>").addClass("translations");
        var node = cleft.find(".node[data-id='" + contentId + "']");
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
                                displayMarkup(null, type, vcode,undefined,contentId);
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
        } else {
            if (contentId != null && contentId != undefined)
                getVariantsForId(contentId, function (d) {
                    for (var i = 0; i < d.length; i++) {
                        (function () {
                            var dtli = $("<li/>");
                            if (!d[i].Published)
                                dtli.addClass("unpublished");
                            if (d[i].Variant != variant) {
                                var lnk = $("<a href='#'/>").html("-" + variantNames[d[i].Variant]);
                                (function () {
                                    var v = d[i].Variant;
                                    lnk.click(function (e) {
                                        e.preventDefault();
                                        displayMarkup(null, type, v,undefined,contentId);
                                    });
                                }());
                                dtli.append(lnk)
                            } else {
                                dtli.append("-" + variantNames[d[i].Variant]);
                            }
                            translations.append(dtli);
                        })();
                    }
                    cright.prepend(translations);
                });
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
                tabHtml += '<div data-group="' + val + '" class="tab-pane ' + (i == 0 ? "active" : "") + '" id="fieldtabs' + i + '"></div>';
            });
            tabHtml += '<div data-group="default" class="tab-pane" id="fieldtabs' + groups.length + '"></div>';
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
                    cright.find(".fieldwrapper[data-fieldname='" + fieldname.split(".").slice(0, -1).join(".") + "']>.editor-field>.fields").append(el);
                else el.appendTo(cright.find("[data-group='default']"));
            });
            cright.find(".fieldwrapper.complex_child").each(function () {
                var el = $(this);
                if (el.find(".fieldwrapper").length == 0 || el.find(".fields").length == 0) {
                    el.addClass("single_field");
                }
            });
            afterDom();
            cright.show();
            cright.find(".fieldtabs:first").click();
            setChangeTracker();
            highlightSelectedNodeById(contentId);
        });
        //publish btn
        if (userRoles.contains("_publish")) {
            cright.find(".content_publish").click(function () {
                cright.find("input:hidden[name='Published']").val("true");
            });
        } else { cright.find(".content_publish").hide(); }
        //udpate btn
        cright.find(".content_update").click(function () {
            cright.find("input:hidden[name='Published']").val("false");
        });
        //preview btn
        if (contentId) {
            cright.find(".content_preview").click(function (e) {
                e.preventDefault();
                window.open("/admin/api/previewguid?id=" + contentId + "&variant=" + variant, "_blank");
            });
        } else { cright.find(".content_preview").hide(); }

        wireForm(cright.find('form'), function (data) {
            msg(true, "content updated");
            getDrawContent(data.parentId, undefined, true, function () {
                var pnode = cleft.find(".node[data-id='" + data.parentId + "']");
                //pnode.find(".expand:first").removeClass("icon-chevron-right").addClass("icon-chevron-down").css({ visibility: "visible" });
                displayMarkup(null, type, variant,undefined,data.id);
            });
        }, function (data) {
            msg(false, data.message);
        });
    }, fromVariant,contentId);
}
var setChangeTracker = function () {
    changed = false;
    cright.find(":input").change(function (e) {
        changed = true;
    });
}
var msg = function (success, str, shouldRemovePreviousMessages) {
    if (shouldRemovePreviousMessages) {
        cmsg.find("div").remove();
    }
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
var _overlayClose = function () {
    $(".overlayinner,.overlay").remove();
    $("body").css({ overflow: "initial" });
    $(document).unbind("keyup");
}

var overlay = function (el, width, height, top, title) {
    var f = undefined;
    overlayClose();
    searchDialogClose();
    var outer = $(".interfaces .overlay_screen").clone().addClass("");
    outer.find(">h1:first").html(title || "")
    outer.css({ left: cright.offset().left - 20 + "px", width: "0px", top: "90px", height: $(window).height() - 90 + "px" });
    if (outer.offset().top < $(document).scrollTop()) {
        outer.css({top:$(document).scrollTop()});
    }
    var close = $('<i class="overlay_close icon-remove-sign"></i>');
    close.click(function () { overlayClose() });
    outer.append(close);
    var inner = outer.find(".inner");
    var clear = $("<div class='clearboth'/>");
    width = width || cright.width() - 10;

    inner.append(el).append(clear);
    cright.append(outer);
    outer.animate({ width: width + "px" }, 200, function () { if (f) f(); afterDom(); });

    $(document).keyup(function (e) {
        if (e.keyCode == 27) { overlayClose(); }
    });
}

var overlayClose = function () {
    cright.find(".overlay_screen").remove();
    $("body").css({ overflow: "initial" });
    $(document).unbind("keyup");
}

var _overlay = function (el, width, height, top) {
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
    $(document).keyup(function (e) {
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
var canChangeMainContent = function () {
    if (cright.find(".fieldwrapper").length > 0 && changed)
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
        var el = $(this);
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

var dirOfPath = function (s) {
    if (s[s.length - 1] == "/")
        return s;
    return s.substring(0, s.lastIndexOf("/") + 1);
}
var isRootItem = function (s) {
    if (s == "00000000-0000-0000-0000-000000000000")
        return true;
    else return false;
    /*var matches = s.match(/\//g);
    if (matches == null)
        return true;
    //throw "isRootItem - invalid input: " + s;
    return matches.length == 1;*/
}
var typeFromChain = function (s) {
    return s.split(" ")[0];
}
var untranslated = function (variants) {
    if (variants == undefined)
        return false;
    var untranslated = [];
    $(languages).each(function () {
        if (!variants.split(",").contains(this.Key))
            untranslated.push(this.Key);
    });
    return untranslated.length == 0 ? false : untranslated;
}

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
var unpublishedVariants = function (id) {
    var variants = [];
    cleft.find(".node[data-id='" + id + "']>.inner>.variant").each(function () {
        if (!$(this).hasClass("published"))
            variants.push($(this).attr("data-variant"));
    });
    return variants.length == 0 ? false : variants;
}
var publishedVariants = function (id) {
    var variants = [];
    cleft.find(".node[data-id='" + id + "']>.inner>.variant").each(function () {
        if ($(this).hasClass("published"))
            variants.push($(this).attr("data-variant"));
    });
    return variants.length == 0 ? false : variants;
}

var setLocalisation = function (p) {
    getLocalisationDialog(p, function (data) {
        overlay(data, 400, 250, undefined, "Localisation");
        var form = $('.overlay_screen form');
        wireForm(form, function (data) {
            overlayClose();
        }, function (data) {
            msg(false, data.message);
        });
    });
}
var setDomainMapping = function (p) {
    getDomainMappingDialog(p, function (data) {
        overlay(data, 500, 250, undefined, "Domain Mapping");
        var form = $('.overlay_screen form');
        wireForm(form, function (data) {
            overlayClose();
        }, function (data) {
            msg(false, data.message);
            overlayClose();
        });
    });
}
var setNotify = function (p) {
    getNotifyDialog(p, function (data) {
        overlay(data, 450, 480, undefined, "Notifications");
        var form = $('.overlay_screen form');
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
    cleft.find(".node[data-path='" + path + "']").addClass("selected");
}
var highlightSelectedNodeById = function (id) {
    cleft.find(".node").removeClass("selected");
    cleft.find(".node[data-id='" + id + "']").addClass("selected");
}
getUserLanguage(function (d) { defaultLanguage = d; });
getUserRoles(function (d) {
    userRoles = d; hideTopNav();
    $(document).ready(function () {
        var index = location.href.indexOf("?");
        var qs = location.href.substring(index);
        if (index != -1 && qs != "") {
            //console.log("qs %o",qs);
            var rp = /\?action=([a-zA-Z0-1]+)&/;
            var action = rp.exec(qs)[1];
            var hash = "#" + action + "?" + qs.replace(rp, "");
            //console.log("hash %o",hash);
            handleHash(hash);
        }
        if (!userRoles.contains("_republish")) $(".republish_entire_site").hide();
    });
});
var pollRepublishStatus = function () {
    getRepublishEntireSiteStatus(function (data) {
        if (data.Message == "complete") {
            msg(true, data.Message);
        } else {
            msg(0, data.Message,true)
            setTimeout(pollRepublishStatus,5000);
        }
    });
}
var republishEntireSite = function () {
    if (confirm("This can take minutes to complete, during which you won't be able to save or publish content, do you want to continue?")) {
        setRepublishEntireSite(function (data) {
            if (data.success) {
                pollRepublishStatus();
            } else {
                pollRepublishStatus();
            };
        });
    }
}
getVariants(function (data) {
    languages = data;
    if (languages.length == 0) {
        onAfterDom(function () {
            msg(0, "take a moment to setup puck. at the very least, choose your languages!");
        });
        $(".menutop .settings").click();
    }
});
getStartId(function (id) {
    startId = id;
    cleft.find("ul.content li:first").attr("data-id", id);
    getStartPath(function (d) {
        startPath = d;
        cleft.find(".startpath").html(d);
        $(".interfaces .tree_container ul.content .node").attr("data-children_path", startPath);
    });
    getDrawContent(id, undefined, true);
});


//});   