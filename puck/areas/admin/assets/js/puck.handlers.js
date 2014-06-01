//bindings
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
            window.scrollTo(0,0);
            getVariants(function (data) {
                languages = data;
            });
        }, function (data) {
            msg(false, data.message);
        });
        setChangeTracker();
    });
});
//root new content button
$(".create_default").show().click(function () { newContent("/"); });
//task list
$(".menutop .tasks").click(function (e) { e.preventDefault(); showTasks(); });
//users
$(".menutop .users").click(function (e) { e.preventDefault(); showUsers(); });
//select state
$(".menutop li").click(function () {
    $(".menutop li").removeClass("selected");
    $(this).addClass("selected");
});
//template tree expand
$(document).on("click", "ul.content.templates li.node i.expand", function () {
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
        getDrawTemplates(node.attr("data-path"), node);
        node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
    }
});
//content tree expand
$(document).on("click", "ul.content:not(.templates) li.node i.expand", function () {
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
        getDrawContent(node.attr("data-children_path"), node, true);
        node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
    }
});
//node settings dropdown
$(document).on("click", "ul.content li.node i.menu", function (e) {
    //display dropdown
    var node = $(this).parents(".node:first");
    var left = node.offset().left;
    var top = node.offset().top + 30;
    var dropdown = $("." + node.parents("ul.content:first").attr("data-dropdown"));
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
    //filter template drop down stuff
    var type = node.attr("data-type");
    if (type == "folder") {
        dropdown.find("a[data-action]").show();
        if (node.attr("data-path") == '/') {
            dropdown.find("a[data-action='template_delete']").hide();
            dropdown.find("a[data-action='template_move']").hide();
        }   
    } else if (type == "file") {
        dropdown.find("a[data-action]").show();
        dropdown.find("a[data-action='template_create']").hide();
        dropdown.find("a[data-action='template_new_folder']").hide();
    }
    //filter notify
    dropdown.find("a[data-action='notify']").parents("li").show();
    //filter translation item
    var totranslate = untranslated(node.attr("data-variants"));
    if (totranslate)
        dropdown.find("a[data-action='translate']").parents("li").show();
    else
        dropdown.find("a[data-action='translate']").parents("li").hide();
    //filter publish/unpublish
    if (publishedVariants(node.attr("data-path")) != false)
        dropdown.find("a[data-action='unpublish']").parents("li").show();
    else
        dropdown.find("a[data-action='unpublish']").parents("li").hide();

    if (unpublishedVariants(node.attr("data-path")) != false)
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

//menu items
$(document).on("click",".node-dropdown a,.template-dropdown a",function () {
    var el = $(this);
    var action = el.attr("data-action");
    var context = el.parents(".puck-dropdown").attr("data-context");
    var node = $(".node[data-id='" + context + "']");
    switch (action) {
        case "template_create":
            var path = node.attr("data-path");
            newTemplate(path);
            break;
        case "template_new_folder":
            var path = node.attr("data-path");
            newTemplateFolder(path);
            break;
        case "template_delete":
            var path = node.attr("data-path");
            var type = node.attr("data-type");
            if (type == "folder")
                deleteTemplateFolder(node.attr("data-name"), path);
            else 
                deleteTemplate(node.attr("data-name"),path);            
            break;
        case "template_move":
            var markup = $(".interfaces .template_tree_container.move").clone();
            var el = markup.find(".node:first");
            overlay(markup);
            $(".overlayinner .msg").html("select new parent node for content <b>" + node.attr("data-name") + "</b>");
            getDrawTemplates(startPath, el);
            markup.on("click", ".node[data-type='folder']>div>span", function (e) {
                var dest_node = $(this).parents(".node:first");
                var from = node.attr("data-path");
                var to = dest_node.attr("data-path");
                if (!confirm("move " + from + " to " + to + " ?")) {
                    return;
                }
                var afterMove = function (d) {
                    if (d.success) {
                        $("ul.templates .node[data-path='" + from + "']").remove();
                        var tonode = $("ul.templates .node[data-path='" + to + "']");
                        console.log({ el: tonode });
                        tonode.find(".expand:first").removeClass("icon-chevron-right").addClass("icon-chevron-down").css({ visibility: "visible" });
                        getDrawTemplates(to);
                    } else {
                        msg(false, d.message);
                    }
                    overlayClose();
                }
                if(node.attr("data-type")=="file")
                    setMoveTemplate(from, to, afterMove);
                else
                    setMoveTemplateFolder(from, to, afterMove);
            });
            break;
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
            var doPublish = function (id, variant, descendants) {
                setPublish(id, variant, descendants, function (data) {
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
            var doUnpublish = function (id, variant, descendants) {
                setUnpublish(id, variant, descendants, function (data) {
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
            revisionsFor(node.attr("data-variants"), node.attr("data-id"));
            break;
        case "cache":
            showCacheInfo(node.attr("data-path"));
            break;
        case "create":
            newContent(node.attr("data-children_path"), node.attr("data-type"));
            break;
        case "move":
            var markup = $(".interfaces .tree_container.move").clone();
            var el = markup.find(".node:first");
            overlay(markup);
            $(".overlayinner .msg").html("select new parent node for content <b>" + node.attr("data-nodename") + "</b>");
            getDrawContent(startPath, el);
            markup.on("click", ".node span", function (e) {
                var dest_node = $(this).parents(".node:first");
                var from = node.attr("data-path");
                var to = dest_node.attr("data-path");
                if (!confirm("move " + from + " to " + to + " ?")) {
                    return;
                }
                setMove(from, to, function (d) {
                    if (d.success) {
                        cleft.find(".node[data-path='" + from + "']").remove();
                        var tonode = cleft.find(".node[data-path='" + to + "']");
                        console.log({ el: tonode });
                        tonode.find(".expand:first").removeClass("icon-chevron-right").addClass("icon-chevron-down").css({ visibility: "visible" });
                        getDrawContent(to + "/");
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
                var fromVariant = variant.clone().attr("name", "fromVariant");
                $(".overlayinner .typecontainer label").html("Translate from version").siblings().hide().after(fromVariant);
                type.val(node.attr("data-type"));
                var variants = node.attr("data-variants").split(",");
                if (variants.length == 1) {
                    $(".overlayinner .typecontainer").hide();
                    $(".overlayinner").css({ height: "170px" });
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
                    displayMarkup(node.attr("data-path"), node.attr("data-type"), variant.val(), fromVariant.val());
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
        case "notify":
            setNotify(node.attr("data-path"));
            break;
    }
});
cleft.on("click", ".node .icon-search", function () {
    var el = $(this).parents(".node:first");
    searchRoot = el.attr("data-path");
    searchDialog(el.attr("data-path"));
});
cleft.find("ul.content").on("click", "li.node span.nodename", function () {
    //get markup
    if (!canChangeMainContent())
        return false;
    var node = $(this).parents(".node:first");
    var firstVariant = node.attr("data-variants").split(",")[0];
    displayMarkup(node.attr("data-path"), node.attr("data-type"), firstVariant);
});
$("button.search").click(function () {
    searchDialog("");
});
//extensions
$.validator.methods.date = function (value, element) {
    {
        if (value == '' || Globalize.parseDate(value, "dd/MM/yyyy HH:mm:ss") != null) {
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
var isFunction = function (functionToCheck) {
    var getType = {};
    return functionToCheck && getType.toString.call(functionToCheck) === '[object Function]';
}
Array.prototype.contains = function (v) {
    var contains = false;
    for (var i = 0; i < this.length; i++)
        if (this[i] == v)
            contains = true;
    return contains;
}
var location_hash = "";
var checkHash = function () {
    if (window.location.hash != location_hash) {
        $(document).trigger("puck.hash_change", {oldHash:location_hash,newHash:window.location.hash});
        location_hash = window.location.hash;        
    }
    setTimeout(checkHash, 500);
}
checkHash(true);
$(document).on("puck.hash_change", function (e,obj) {
    handleHash(obj.newHash);
    //msg(false, "old hash " + obj.oldHash + "|| new hash " + obj.newHash + " " + Math.random());
});
var handleHash = function (hash) {
    if (/^#content/.test(hash)) {
        var h = hash.replace("#content?", "");
        var kvp = h.split("&");
        var path;
        var variant;
        for (var i = 0; i < kvp.length;i++){
            var k = kvp[i].split("=")[0];
            var v = kvp[i].split("=")[1];
            if (k == "path")
                path = v;
            if (k == "variant")
                variant = v;
        }
        displayMarkup(path,"",variant);
    } else if (/^#settings/.test(hash)) {
        $(".menutop .settings").click();
    } else if (/^#users/.test(hash)) {
        $(".menutop .users").click();
    } else if (/^#tasks/.test(hash)) {
        $(".menutop .tasks").click();
    }
}
$(document).ready(function () {
    handleHash(window.location.hash);
});