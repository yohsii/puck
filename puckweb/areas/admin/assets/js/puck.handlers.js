﻿//bindings
//tabs
$(document).on("click", ".editor-field .nav-tabs li a", function (e) {
    e.preventDefault();
});
//handle tabs without needing to set hrefs and ids
$(document).off("click.tabs").on("click.tabs", ".editor-field .nav-tabs li", function () {
    var el = $(this);
    var tabsContainer = el.parent().parent();
    tabsContainer.find(".nav-tabs li a").removeClass("active");
    el.find("a").addClass("active");
    var index = el.index() + 1;
    tabsContainer.find(".tab-content>div").removeClass("active");
    tabsContainer.find(".tab-content>div:nth-child(" + index + ")").addClass("active");
    //console.log("index",el.index());
});

//$('a.settings').click(function (e) {
//    e.preventDefault();
//    if (!canChangeMainContent())
//        return false;
//    getSettings(function (data) {
//        cright.html(data);
//        afterDom();
//        //setup validation
//        wireForm(cright.find('form'), function (data) {
//            msg(true, "settings updated.");
//            window.scrollTo(0,0);
//            getVariants(function (data) {
//                languages = data;
//            });
//        }, function (data) {
//            msg(false, data.message);
//        });
//        setChangeTracker();
//    });
//});
$("html").on("click", ".left_settings li button", function (e) {
    //if (!canChangeMainContent())
    //    return false;
    //var el = $(this).parent();
    //var path = el.attr("data-path");
    //showSettings(path);
});
//root new content button
$(".create_default").show().click(function () { newContent("00000000-0000-0000-0000-000000000000"); });
//task list
//$(".menutop .tasks").click(function (e) { e.preventDefault(); showTasks(); });
//users
//$(".menutop .users").click(function (e) { e.preventDefault(); showUsers(); });
//select state
$(".menutop li").click(function () {
    $(".menutop li").removeClass("selected");
    $(this).addClass("selected");
});
//republish entire site button
$(".republish_entire_site").click(function () { republishEntireSite(); });

//template tree expand
$(document).on("click", "ul.content.templates li.node i.expand", function () {
    //get children content
    var node = $(this).parents(".node:first");
    var descendants = node.find("ul");
    if (descendants.length > 0) {//show
        if (descendants.first().is(":hidden")) {
            node.find("i.expand").removeClass("fa-chevron-right").addClass("fa-chevron-down");
            descendants.show();
        } else {//hide
            node.find("i.expand").removeClass("fa-chevron-down").addClass("fa-chevron-right");
            descendants.hide();
        }
    } else {
        getDrawTemplates(node.attr("data-path"), node);
        node.find("i.expand").removeClass("fa-chevron-right").addClass("fa-chevron-down");
    }
});
//content tree expand
$(document).on("click", "ul.content:not(.templates) li.node i.expand", function () {
    //get children content
    var node = $(this).parents(".node:first");
    var descendants = node.find("ul");
    if (descendants.length > 0) {//show
        if (descendants.first().is(":hidden")) {
            node.find("i.expand").removeClass("fa-chevron-right").addClass("fa-chevron-down");
            descendants.show();
        } else {//hide
            node.find("i.expand").removeClass("fa-chevron-down").addClass("fa-chevron-right");
            descendants.hide();
        }
    } else {
        getDrawContent(node.attr("data-id"), node, true);
        node.find("i.expand").removeClass("fa-chevron-right").addClass("fa-chevron-down");
    }
});
//node settings dropdown
$(document).on("click", "ul.content li.node i.menu", function (e) {
    //display dropdown
    var node = $(this).parents(".node:first");
    var left = node.position().left;
    var top = node.position().top+15;
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
    if (publishedVariants(node.attr("data-id")) != false)
        dropdown.find("a[data-action='unpublish']").parents("li").show();
    else
        dropdown.find("a[data-action='unpublish']").parents("li").hide();

    if (unpublishedVariants(node.attr("data-id")) != false)
        dropdown.find("a[data-action='publish']").parents("li").show();
    else
        dropdown.find("a[data-action='publish']").parents("li").hide();
    //filter domain
    if (isRootItem(node.attr("data-parent_id"))) {
        dropdown.find("a[data-action='domain']").parents("li").show();
    } else {
        dropdown.find("a[data-action='domain']").parents("li").hide();
    }
    //filter move - disallow root move
    if (isRootItem(node.attr("data-parent_id")))
        dropdown.find("a[data-action='move']").parents("li").hide();
    else
        dropdown.find("a[data-action='move']").parents("li").show();
    //filter copy - disallow root copy
    if (isRootItem(node.attr("data-parent_id")))
        dropdown.find("a[data-action='copy']").parents("li").hide();
    else
        dropdown.find("a[data-action='copy']").parents("li").show();
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
            overlay(markup,undefined,undefined,undefined,"Move Template");
            $(".overlay_screen .msg").html("select new parent node for content <b>" + node.attr("data-name") + "</b>");
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
                        tonode.find(".expand:first").removeClass("fa-chevron-right").addClass("fa-chevron-down").css({ visibility: "visible" });
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
                            getDrawContent(node.attr("data-parent_id"), undefined, undefined, function () {
                                highlightSelectedNode(node.attr("data-id"));
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
                    overlay(dialog, 400, 150,undefined,"Delete");
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
                        getDrawContent(node.attr("data-parent_id"), undefined, true);
                        node.find(">.inner>.variant[data-variant='"+variant+"']").addClass("published");
                        overlayClose();
                    } else {
                        msg(false, data.message);
                        overlayClose();
                    }
                });
            }
            var variants = unpublishedVariants(node.attr("data-id"));
            if (variants.length > 1||true ) {
                var dialog = dialogForVariants(variants);
                dialog.find(".descendantscontainer label").html("Publish descendants?");
                overlay(dialog, 400, 250,undefined,"Publish");
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
                        getDrawContent(node.attr("data-parent_id"), undefined, true);
                        node.find(">.inner>.variant[data-variant='" + variant + "']").removeClass("published");
                        publishedContent[id][variant] = undefined;
                        overlayClose();
                    } else {
                        msg(false, data.message);
                        overlayClose();
                    }
                });
            }
            var variants = publishedVariants(node.attr("data-id"));
            if (variants.length > 1 || 1 == 1) {
                var dialog = dialogForVariants(variants);
                dialog.find(".descendantscontainer").hide();
                dialog.find(".descendantscontainer label").html("Unpublish descendants?");
                overlay(dialog, 400, 250,undefined,"Unpublish");
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
            newContent(node.attr("data-id"), node.attr("data-type"));
            break;
        case "move":
            var markup = $(".interfaces .tree_container.move").clone();
            var el = markup.find(".node:first");
            overlay(markup,undefined,undefined,undefined,"Move Content");
            $(".overlay_screen .msg").html("select new parent node for content <b>" + node.attr("data-nodename") + "</b>");
            getDrawContent(startId, el);
            markup.on("click", ".node span", function (e) {
                var dest_node = $(this).parents(".node:first");
                var from = node.attr("data-path");
                var to = dest_node.attr("data-path");
                var fromId = node.attr("data-id");
                var toId = dest_node.attr("data-id");
                if (!confirm("move " + from + " to " + to + " ?")) {
                    return;
                }
                setMove(fromId, toId, function (d) {
                    if (d.success) {
                        cleft.find(".node[data-id='" + fromId + "']").remove();
                        var tonode = cleft.find(".node[data-id='" + toId + "']");
                        console.log({ el: tonode });
                        tonode.find(".expand:first").removeClass("fa-chevron-right").addClass("fa-chevron-down").css({ visibility: "visible" });
                        getDrawContent(toId);
                    } else {
                        msg(false, d.message);
                    }
                    overlayClose();
                });
            });
            break;
        case "copy":
            var markup = $(".interfaces .tree_container.copy").clone();
            var el = markup.find(".node:first");
            overlay(markup, undefined, undefined, undefined, "Copy Content");
            $(".overlay_screen .msg").html("select new parent node for copied content <b>" + node.attr("data-nodename") + "</b>");
            getDrawContent(startId, el);
            markup.on("click", ".node span", function (e) {
                var dest_node = $(this).parents(".node:first");
                var from = node.attr("data-path");
                var to = dest_node.attr("data-path");
                var fromId = node.attr("data-id");
                var toId = dest_node.attr("data-id");
                var nodeTitle = node.find("span:first").text();
                var includeDescendants = markup.find("input").is(":checked");
                console.log("includeDescendants",includeDescendants);
                if (!confirm("copy " + nodeTitle + " to " + to + " ?")) {
                    return;
                }
                setCopy(fromId, toId, includeDescendants, function (d) {
                    if (d.success) {
                        var tonode = cleft.find(".node[data-id='" + toId + "']");
                        console.log({ el: tonode });
                        if (tonode.length == 0) return;
                        tonode.find(".expand:first").removeClass("fa-chevron-right").addClass("fa-chevron-down").css({ visibility: "visible" });
                        getDrawContent(toId);
                    } else {
                        msg(false, d.message);
                    }
                    overlayClose();
                });
            });
            break;
        case "changetype":
            getChangeTypeDialog(node.attr("data-id"),function (markup) {
                var variants = node.attr("data-variants").split(",");
                overlay(markup, 400, 250, undefined, "Change Type");
                $(".overlay_screen button").click(function () {
                    var newType = $(".overlay_screen select").val();
                    getChangeTypeMappingDialog(node.attr("data-id"), newType, function (mappingMarkup) {
                        overlay(mappingMarkup, 500, 250, undefined, "Change Type");
                        wireForm($(".overlay_screen form"), function (d) {
                            msg(true, "type changed");
                            displayMarkup(null, node.attr("data-type"), variants[0], undefined, node.attr("data-id"));
                            overlayClose();
                        }, function (d) {
                            msg(false, d.message);
                            overlayClose();
                        });
                    });
                });
                
            });
            break;
        case "timedpublish":
            timedPublish(node.attr("data-variants"),node.attr("data-id"));
            break;
        case "audit":
            showAudit(node.attr("data-id"),"","",1,20,cright);
            break;
        case "translate":
            getCreateDialog(function (data) {
                overlay(data, 400, 250,undefined,"Translate");
                var type = $(".overlay_screen select[name=type]");
                var variant = $(".overlay_screen select[name=variant]");
                var fromVariant = variant.clone().attr("name", "fromVariant");
                $(".overlay_screen .typecontainer label").html("Translate from version").siblings().hide().after(fromVariant);
                type.val(node.attr("data-type"));
                var variants = node.attr("data-variants").split(",");
                if (variants.length == 1) {
                    $(".overlay_screen .typecontainer").hide();
                    //$(".overlay_screen").css({ height: "170px" });
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
                $(".overlay_screen button").click(function () {
                    displayMarkup(null, node.attr("data-type"), variant.val(), fromVariant.val(),node.attr("data-id"));
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
cleft.on("click", ".node .icon_search", function () {
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
    location.hash = "#content?id=" + node.attr("data-id") + "&variant=" + firstVariant;
    //displayMarkup(null, node.attr("data-type"), firstVariant, undefined, node.attr("data-id"));
});
$("button.search").click(function () {
    searchDialog("");
});

//set heights
var setAreaHeights = function () {
    var _h = $(window).height() - ($(".menutop").outerHeight() + 15);
    $(".leftarea").css({ height: _h, overflowY: "scroll" });
    $(".rightarea").css({ height: _h, overflowY: "scroll" });
    $(".leftToggle i").css({ top: (Math.round(_h / 2)) });
}
setAreaHeights();

var toggleMobileUI = function () {
    if ($(window).width() < 768) {
        $(".leftToggle").show();
        cleft.css({ position: "absolute" });
        $(".main.grid").on("click.mobileUi", ".rightarea", function (e) {
            if ($.contains($(".search_ops").get(0), e.target)) return;
            if ($.contains($(".overlay_screen").get(0), e.target)) return;
            cleft.hide();
        });
        $(".leftToggle").off().click(function (e) {
            e.stopPropagation();
            cleft.show();
        });
    } else {
        cleft.show();
        $(".leftToggle").hide();
        cleft.css({ position: "relative" });
        $(".main.grid").off("click.mobileUi");

    }
}

$(window).resize(function () { setAreaHeights(); toggleMobileUI(); });

toggleMobileUI();
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
//checkHash(true);
$(window).on("hashchange", function (e) {
    handleHash(location.hash);
    //msg(false, "old hash " + obj.oldHash + "|| new hash " + obj.newHash + " " + Math.random());
});
$(document).on("puck.hash_change", function (e,obj) {
    handleHash(obj.newHash);
    //msg(false, "old hash " + obj.oldHash + "|| new hash " + obj.newHash + " " + Math.random());
});
var getHashValues = function (hash) {
    var h = hash.substring(hash.indexOf("?")+1);
    var kvp = h.split("&");
    var dict = [];
    for (var i = 0; i < kvp.length; i++) {
        var k = kvp[i].split("=")[0];
        var v = kvp[i].split("=")[1];
        dict[k] = v;
    }
    return dict;
}
var highlightSection = function (href) {
    $(".menutop li").removeClass("selected");
    var anchor = $(".menutop a[href='" + href + "']");
    var el = anchor.parent();
    el.addClass("selected");
}
var handleHash = function (hash) {
    if (/^#content/.test(hash)) {
        highlightSection("#content");
        $(".left_item").hide();
        cleft.find(".left_content").show();
        var dict = getHashValues(hash);
        if (dict["id"] == undefined || dict["variant"] == undefined) {
            cright.html("");
            return;
        }
        displayMarkup(null, null, dict["variant"], undefined, dict["id"]);
    } else if (/^#settings/.test(hash)) {
        highlightSection("#settings");
        //if (!canChangeMainContent())
        //    return false;
        $(".left_item").hide();
        cleft.find(".left_settings").show();
        var dict = getHashValues(hash);
        var path = dict["path"];
        cleft.find(".left_settings a").removeClass("current");
        cleft.find(".left_settings a[href='"+hash+"']").addClass("current");
        showSettings(path);
        $(".menutop .settings").click();
    } else if (/^#users/.test(hash)) {
        highlightSection("#users");
        $(".left_item").hide();
        cleft.find(".left_users").show();
        showUsers();
    } else if (/^#developer/.test(hash)) {
        highlightSection("#developer");
        $(".left_item").hide();
        cleft.find(".left_developer").show();
        showTasks();
    }
}
$(window).load(function () {
    
});