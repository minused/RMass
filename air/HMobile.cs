using System;

namespace com.sulake.habboair
{
    public static class MApi
    {
        // SWF Location
        // scripts/§_-0GA§.§_-1Bm§

        public static String force_email_change = "/api/force/email-change";

        public static String force_password_change = "/api/force/password-change";

        public static String force_tos_accept = "/api/force/tos-accept";

        public static String authentication_facebook = "/api/public/authentication/facebook";

        public static String authentication_rpx = "/api/public/authentication/rpx";

        public static String preferences_save = "/api/user/preferences/save";

        public static String authentication_user = "/api/public/authentication/user";

        public static String preferences_save_visibility = "/api/user/preferences/save/visibility";

        public static String forgot_password_change_password = "/api/public/forgotPassword/changePassword";

        public static String campaign_messages = "/api/user/campaign_messages";

        public static String groups = "/api/public/groups/:id";

        public static String avatars_select = "/api/user/avatars/select";

        public static String campaign_messages_all = "/api/user/campaign_messages/all";

        public static String groups_members = "/api/public/groups/:id/members";

        public static String registration_activate = "/api/public/registration/activate";

        public static String campaign_messages_seen = "/api/user/campaign_messages/seen";

        public static String rooms_popular = "/api/public/rooms/popular";

        public static String discussions = "/api/user/discussions";

        public static String user_profile = "/api/user/profile";

        public static String credit_balance = "/api/user/credit_balance";

        public static String lists_hotlooks = "/api/public/lists/hotlooks";

        public static String rooms = "/api/public/rooms/:roomId";

        public static String avatars = "/api/user/avatars";

        public static String friendrequests_sent = "/api/user/friendrequests/sent";

        public static String log_crash = "/api/log/crash";

        public static String friendrequests_received = "/api/user/friendrequests/received";

        public static String log_loginstep = "/api/log/loginstep";

        public static String look_save = "/api/user/look/save";

        public static String client_url = "/api/client/clienturl";

        public static String info_hello = "/api/public/info/hello";

        public static String email_change = "/api/user/email/change";

        public static String newuser_name_check = "/api/newuser/name/check";

        public static String log_error = "/api/log/error";

        public static String user_avatars = "/api/user/avatars";

        public static String newuser_name_select = "/api/newuser/name/select";

        public static String ssotoken = "/api/ssotoken";

        public static String newuser_room_select = "/api/newuser/room/select";

        public static String safetylock_featurestatus = "/api/safetylock/featureStatus";

        public static String authentication_login = "/api/public/authentication/login";

        public static String safetylock_disable = "/api/safetylock/disable";

        public static String registration_new = "/api/public/registration/new";

        public static String safetylock_save = "/api/safetylock/save";

        public static String safetylock_resettrustedlogins = "/api/safetylock/resetTrustedLogins";

        public static String safetylock_questions = "/api/safetylock/questions";

        public static String authentication_logout = "/api/public/authentication/logout";

        public static String forgotpassword_send = "/api/public/forgotPassword/send";

        public static String safetylock_unlock = "/api/safetylock/unlock";

        public static String user_preferences = "/api/user/preferences";

        public static String common_friends = "/api/user/:id/common_friends";

        public static String user_ping = "/api/user/ping";

        public static String user_self = "/api/user/self";

        public static String iap_itunes_validate = "/shopapi/iap/itunes/validate";

        public static String iap_playstore_validate = "/shopapi/iap/playstore/validate";

        public static String pushwoosh_devicetoken = "/api/pushwoosh/devicetoken";
    }
}