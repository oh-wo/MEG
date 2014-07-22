//for index pages
$(document).on('click', '.e-rsvp', function (e) {
    e.stopPropagation();
    var articleID = $(this).closest('.e-container').attr('articleID');
    $('<div id="cover" style="top:'+posTop()+'px;"></div>').appendTo('body');
    $(this).closest('.e-container').find('.rsvpContainer').offset({ top: (posTop() + ($(window).height() - $($('.rsvpContainer')[0]).height()) / 3), left: ($(window).width() - $($('.rsvpContainer')[0]).width()) / 2 }).fadeIn(200);
    $('.rsvpContainer:visible').offset({ top: 100, left: ($(window).width() - $($('.rsvpContainer')[0]).width()) / 2 });
    $('body').css('overflow', 'hidden');
});
//article page
$(document).on('click', '#rsvp', function (e) {
    $('.rsvpContainer').css('top', ((posTop() + ($(window).height() - $($('.rsvpContainer')[0]).height()) / 3) + 'px')).css('left', ($(window).width() - $($('.rsvpContainer')[0]).width()) / 2).fadeIn(200);
    $('<div id="cover" style="top:' + posTop() + 'px;"></div>').appendTo('body');
    $('body').css('overflow', 'hidden');
});

$(document).on('change', '.notAMember', function (e) {
    var $this = $(this);
    if ($this.attr('checked') == 'checked') {
        $this.closest('tbody').find('input[type="checkbox"]:not(.notAMember)').removeAttr('checked');
    }

})

function posTop() {
    return typeof window.pageYOffset != 'undefined' ? window.pageYOffset : document.documentElement && document.documentElement.scrollTop ? document.documentElement.scrollTop : document.body.scrollTop ? document.body.scrollTop : 0;
}
    $(document).on('click', '.completeRsvp', function (e) {
        
        var cont = $(this).closest('.rsvpContainer');
        $(cont).find('.userNameValidationError').fadeOut(100);
        $(cont).find('.userEmailValidationError').fadeOut(100);
        var validationErrors = false;
        if ($(cont).find('.rsvpUserName').val() == '') { $(cont).find('.userNameValidationError').fadeIn(); validationErrors = true; }
        if ($(cont).find('.rsvpUserEmail').val() == '') { $(cont).find('.userEmailValidationError').fadeIn(); validationErrors = true; }
        if (!validationErrors) {
            ReserveSeats(cont);
        } else {
            //wait for user to correct errors
        }
    });
    $(document).on('click', '.rsvpCloseWindow', function (e) {
    $(this).closest('.rsvpContainer').fadeOut(200);
    $('#cover').fadeOut(200,function(){
        $(this).remove();
    });
    $('body').css('overflow', 'auto');
    });
    $(document).on('click', '.rsvpContainer', function (e) { e.stopPropagation();})
    function ReserveSeats(cont) {
        $(cont).find('.completeRsvp').val('Saving..');
        $(cont).find('input').attr('disabled', 'disabled');

        var EventID = $(cont).closest('.e-container').attr('articleid');//for the index pages
        if(EventID==undefined){
            //For the article page
            EventID=$('#articleContainer').attr('articleid');
        }

    var data = {
        EventID: EventID,
        Name: $(cont).find('.rsvpUserName').val(),
        Email:$(cont).find('.rsvpUserEmail').val(),
        NoSeats:parseInt($(cont).find('.rsvpNoSeats').val()),
        MemberMeg:$(cont).find('.MegMember:checked').length>0,
        MemberIpenz:$(cont).find('.IpenzMember:checked').length>0,
        MemberImeche:$(cont).find('.ImecheMember:checked').length>0,

    };

    var dataToPost = JSON.stringify(data);

    $.ajax({
        type: "POST",
        url: "/Home/CreateReservation",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: dataToPost,
        success: function (a) {
            if (a.Success) {
                //saved ok
                var string = "<div class='rsvpCloseWindow' style='float: right;'>";
                string += "<img src='/Content/black-cross-md.png' class='rsvpCloseImage'>";
                string += "</div>";
                string += "<div class='rsvpComplete'>Thanks! We've sent you an email confirming your reservation.</div>";
                if (!a.OnEmailList) {
                    //user's email isn't in the mailing list
                    string += '<div class="AddToMailingList">Add me to the MEG mailing list<div>';
                    $(cont).animate({ height: '70px' }, 500);
                } else {
                    //this email address is already in the mailing list
                    $(cont).animate({ height: '20px' }, 500);
                }
                $(cont).children().hide();
                $(string).appendTo(cont);
                $(cont).closest('.e-container').find('.e-rsvp').attr('disabled', 'disabled').attr('title','You\'ve already rsvp\'d to this event. Go to your emails to modify your reservation');
                
            } else {
                //didn't save ok
                var string = "<div class='rsvpCloseWindow' style='float: right;'>";
                string += "<img src='/Content/black-cross-md.png' class='rsvpCloseImage'>";
                string += "</div>";
                string += "<div id='complete' style='float:left;'>Not saved!</div>";
                string += "<div style='float:left;'>Sorry there appears to have been an error and we couldn't save your reservation. Please reload the page and try again.</div>";


                $(cont).html(string);
            }
        },
        error: function (ex) {
        },
    });
    }
    $(document).on('click', '.AddToMailingList', function (e) {
        $('.AddToMailingList').animate({ opacity: 0.5 }, 200);
        var cont = $(this).closest('.rsvpContainer');
        var data = {
            Name: $(cont).find('.rsvpUserName').val(),
            Email: $(cont).find('.rsvpUserEmail').val(),

        };

        var dataToPost = JSON.stringify(data);

        $.ajax({
            type: "POST",
            url: "/Home/AddToMailingList",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: dataToPost,
            success: function (a) {
                if (a) {
                    //saved ok
                    var string = "<div class='rsvpCloseWindow' style='float: right;'>";
                    string += "<img src='/Content/black-cross-md.png' class='rsvpCloseImage'>";
                    string += "</div>";
                    string += "<div class='mailingComplete'>Thanks! You've been added to the mailing list.</div>";
                    $(cont).children().hide();
                    $(string).appendTo(cont);
                    $(cont).animate({ height: '20px' }, 500);
                } else {
                    //didn't save ok
                    var string = "<div class='rsvpCloseWindow' style='float: right;'>";
                    string += "<img src='/Content/black-cross-md.png' class='rsvpCloseImage'>";
                    string += "</div>";
                    string += "<div id='mailingComplete' style='float:left;'>Not saved!</div>";
                    string += "<div style='float:left;'>Sorry there appears to have been an error and we couldn't save you to the mailing list. Please reload the page and try again.</div>";
                    
                   
                    $(cont).html(string);
                }
            },
            error: function (ex) {
             
            },
        });
    });