using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class
{
    public class FileParser
    {
        public static readonly List<(int Position, int Length, string Header)> DemColumns = new List<(int, int, string)>
        {
            (0,   7,  "household_id"),           // First 7 bytes
            (7,   2,  "member_id"),              // 2 bytes
            (9,   8,  "weight"),                 // 8 bytes
            (17,  1,  "base_panel_code"),        // 1 byte
            (18,  1,  "viewer_type"),            // 1 byte
            (19,  1,  "sample_type"),            // 1 byte
            (20,  1, "time_zone"),                      // 1 byte            
            (21,  1, "home_ownership"),                 // 1 byte
            (22,  1, "household_moved"),                // 1 byte
            (23,  1, "new_car_in_past_2_yrs"),          // 1 byte
            (24,  1, "used_car_in_past_2_yrs"),         // 1 byte
            (25,  1, "vehicle_3+_yrs"),                 // 1 byte
            (26,  1, "owns_domestic_vehicle"),          // 1 byte
            (27,  1, "owns_imported_vehicle"),          // 1 byte
            (31,  1, "presence_children_less2"),        // 1 byte
            (32,  1, "size_of_household"),              // 1 byte
            (33,  1, "sex_at_birth"),                   // 1 byte
            (34,  1, "age"),                            // 1 byte
            (37,  1, "household_member_status"),        // 1 byte
            (38,  1, "hours_worked"),                   // 1 byte
            (39,  1, "non_working_categories"),         // 1 byte
            (42,  1, "main_shopper"),                   // 1 byte
            (57,  1, "a_mortgage"),                     // 1 byte
            (59,  1, "registered_retirement_savings"),  // 1 byte
            (63,  1, "households_with_kids_less12"),    // 1 byte
            (65,  1, "length_of_time_on_panel"),        // 1 byte
            (67,  1, "age_range_tv_2017"),              // 1 byte
            (68,  1, "language_of_conversations"),      // 1 byte
            (70,  1, "indiv_2+_with_k2_5"),             // 1 byte
            (71,  1, "indiv_2+_with_k6_11"),            // 1 byte
            (72,  1, "indiv_2+_with_t12_17"),           // 1 byte
            (73,  1, "working_tv_sets"),                // 1 byte
            (74,  1, "cable_provider"),                 // 1 byte
            (75,  1, "pvr"),                            // 1 byte
            (79,  1, "relation_to_hoh"),                // 1 byte
            (82,  1, "satellite_radio"),                // 1 byte
            (86,  1, "occupation"),                     // 1 byte
            (90,  1, "hw"),                             // 1 byte
            (91,  1, "a18+_at_home"),                   // 1 byte
            (92,  1, "c12_17_at_home"),                 // 1 byte
            (93,  1, "c2_6_at_home"),                   // 1 byte
            (94,  1, "c7_11_at_home"),                  // 1 byte
            (95,  1, "cless2_at_home"),                 // 1 byte
            (96,  1, "managers_owners_professionals"),  // 1 byte
            (97,  1, "presence_of_vehicle"),            // 1 byte
            (98,  1, "bbm_nmr_region"),                 // 1 byte
            (99,  1, "dwelling_type"),                  // 1 byte
            (101, 1, "vehicle_price"),                  // 1 byte
            (103, 1, "tv_set_connected_to_cable"),      // 1 byte
            (104, 1, "tv_connected_to_digital_box"),    // 1 byte
            (105, 1, "tv_connected_to_satellite"),      // 1 byte
            (106, 1, "view_more_digibox_or_satel_set"), // 1 byte
            (107, 1, "tv_connctd_to_off_air_antenna"),  // 1 byte
            (108, 1, "reception_provider_digibox"),     // 1 byte
            (109, 1, "reception_provider_satellite"),   // 1 byte
            (119, 1, "marital_status"),                 // 1 byte
            (120, 1, "daily_transportation"),           // 1 byte
            (121, 1, "education"),                      // 1 byte
            (188, 1, "personal_line_of_credit"),        // 1 byte
            (189, 1, "registered_education_savings"),   // 1 byte
            (190, 1, "registered_retirement_income"),   // 1 byte
            (201, 1, "internet_hours_past_7_days"),     // 1 byte
            (220, 1, "travel_own_province_personal"),   // 1 byte
            (221, 1, "travel_out_of_province_persona"), // 1 byte
            (222, 1, "travel_us_personal"),             // 1 byte
            (223, 1, "other_travel_personal"),          // 1 byte
            (225, 1, "cable_summary_2011___reporting"), // 1 byte
            (227, 1, "2017_first_language_learned"),    // 1 byte
            (228, 1, "2017langspomostoftnhomeret2026"), // 1 byte
            (247, 1, "oil_change_speclty_auto_centr"),  // 1 byte
            (248, 1, "oil_change_auto_dealership"),     // 1 byte
            (251, 1, "tune_up_specialty_auto_center"),  // 1 byte
            (252, 1, "tune_up_auto_dealership"),        // 1 byte
            (255, 1, "replmufflr_exhstspecltyautocen"), // 1 byte
            (256, 1, "repl_mufflr_exhst_auto_dlrshp"),  // 1 byte
            (259, 1, "rep._brakes_speclty_auto_centr"), // 1 byte
            (260, 1, "repair_brakes_auto_dealership"),  // 1 byte
            (263, 1, "repl_chng_tires_specltyautocen"), // 1 byte
            (264, 1, "repl_chng_tires_auto_dealrshp"),  // 1 byte
            (267, 1, "repl_repairwndshld_specautocen"), // 1 byte
            (268, 1, "repl_repairwndshld_auto_dlrshp"), // 1 byte
            (284, 1, "work_tv"),                        // 1 byte
            (285, 1, "work_radio"),                     // 1 byte
            (286, 1, "work_internet"),                  // 1 byte
            (287, 1, "private_vehicle_tv"),             // 1 byte
            (288, 1, "private_vehicle_radio"),          // 1 byte
            (289, 1, "private_vehicle_internet"),       // 1 byte
            (290, 1, "school_library_tv"),              // 1 byte
            (291, 1, "school_library_radio"),           // 1 byte
            (292, 1, "school_library_internet"),        // 1 byte
            (293, 1, "public_transit_taxi_tv"),         // 1 byte
            (294, 1, "public_transit_taxi_radio"),      // 1 byte
            (295, 1, "public_transit_taxi_internet"),   // 1 byte
            (296, 1, "other_location_tv"),              // 1 byte
            (297, 1, "other_location_radio"),           // 1 byte
            (298, 1, "other_location_internet"),        // 1 byte
            (300, 1, "cycling_in_season"),              // 1 byte
            (301, 1, "dwnhilskiing_snowbrdg_in_seasn"), // 1 byte
            (303, 1, "fishing_hunting_in_season"),      // 1 byte
            (304, 1, "gardening_in_season"),            // 1 byte
            (305, 1, "golf_in_season"),                 // 1 byte
            (306, 1, "hiking_camping_in_season"),       // 1 byte
            (307, 1, "hockey_ice_skating_in_season"),   // 1 byte
            (308, 1, "pwrboatg_sailg_jetski_in_seasn"), // 1 byte
            (309, 1, "riding_snowmobile_atv_in_seasn"), // 1 byte
            (310, 1, "oth_indv_team_sports_in_season"), // 1 byte
            (320, 1, "ppm_gen"),                        // 1 byte
            (321, 1, "mobile_only_home"),               // 1 byte
            (322, 1, "lifestage_group_2015"),           // 1 byte
            (323, 1, "2014_industry"),                  // 1 byte
            (324, 1, "cable_summary_2016"),             // 1 byte
            (325, 1, "high_speed_internet_2016"),       // 1 byte
            (326, 1, "income_2016"),                    // 1 byte
            (327, 1, "pets_dog(s)"),                    // 1 byte
            (328, 1, "pets_cat(s)"),                    // 1 byte
            (329, 1, "pets_other"),                     // 1 byte
            (330, 1, "recreational_properties"),        // 1 byte
            (331, 1, "home_improvements_cost_2016"),    // 1 byte
            (332, 1, "home_improvement_none_past_2y"),  // 1 byte
            (333, 1, "home_improve_repl_window_doors"), // 1 byte
            (334, 1, "home_improve_carpets_flooring"),  // 1 byte
            (335, 1, "home_improvements_roof"),         // 1 byte
            (336, 1, "home_improvements_home_decor"),   // 1 byte
            (337, 1, "home_improve_electricl_lightin"), // 1 byte
            (338, 1, "home_improve_kitch_renovation"),  // 1 byte
            (339, 1, "home_improve_bathrm_renovate"),   // 1 byte
            (340, 1, "home_improve_energy_efficient"),  // 1 byte
            (341, 1, "home_improve_landscaping"),       // 1 byte
            (342, 1, "home_improve_major_renovation"),  // 1 byte
            (343, 1, "home_improve_replaced_mjr_appl"), // 1 byte
            (344, 1, "grocery_spending_weekly_2016"),   // 1 byte
            (346, 1, "oil_change_gas_srvc_st_mechn"),   // 1 byte
            (347, 1, "oil_change_yourself_rela_frnd"),  // 1 byte
            (349, 1, "tune_up_gas_srvc_st_mechn"),      // 1 byte
            (350, 1, "tune_up_yourself_rela_frnd"),     // 1 byte
            (352, 1, "replmufflr_gas_srvc_st_mechn"),   // 1 byte
            (353, 1, "replmufflr_yourself_rela_frnd"),  // 1 byte
            (355, 1, "rep.brakes_gas_srvc_st_mechn"),   // 1 byte
            (356, 1, "rep.brakes_yourself_rela_frnd"),  // 1 byte
            (358, 1, "repl_chg_tre_gas_srvc_st_mechn"), // 1 byte
            (359, 1, "repl_chg_tre_yourself_rel_frnd"), // 1 byte
            (361, 1, "rpl_rpr_wdshd_gas_srv_st_mechn"), // 1 byte
            (362, 1, "rpl_repr_wdshd_urself_rel_frnd"), // 1 byte
            (364, 1, "autobdy_repr_gas_srv_st_mechn"),  // 1 byte
            (365, 1, "autobdy_repr_yourself_rel_frnd"), // 1 byte
            (366, 1, "autobdy_repr_rt.streautocentr"),  // 1 byte
            (367, 1, "autobdy_repr_auto_dealrshp"),     // 1 byte
            (382, 1, "movie_theatre_12m"),              // 1 byte
            (383, 1, "live_balle_opera_art_musm_12m"),  // 1 byte
            (384, 1, "professional_sport_event_12m"),   // 1 byte
            (385, 1, "musical_concerts_12m"),           // 1 byte
            (386, 1, "casino_12m"),                     // 1 byte
            (387, 1, "consumer_shows_12m"),             // 1 byte
            (388, 1, "smartphone"),                     // 1 byte
            (389, 1, "othr_mobile_phone"),              // 1 byte
            (390, 1, "tablet"),                         // 1 byte
            (391, 1, "wearabledev_smrtwtch_fitmonetc"), // 1 byte
            (409, 1, "driving_distance_annually"),      // 1 byte
            (410, 1, "travel_cari_mex_centr_s.a_pers"), // 1 byte
            (412, 1, "travel_europe_personal"),         // 1 byte
            (414, 1, "automobile_loan_financing"),      // 1 byte
            (415, 1, "gic_term_dep_govt_sav_bond"),     // 1 byte
            (416, 1, "mutual_funds"),                   // 1 byte
            (417, 1, "online_banking"),                 // 1 byte
            (418, 1, "overdraft_protection"),           // 1 byte
            (419, 1, "stocks_bonds_2016"),              // 1 byte
            (420, 1, "tax_free_savings_account(tfsa)"), // 1 byte
            (421, 1, "financial_planner"),              // 1 byte
            (422, 1, "tax_preparation_service"),        // 1 byte
            (423, 1, "will_estate_planner"),            // 1 byte
            (424, 1, "aerobics_working_out_2016"),      // 1 byte
            (425, 1, "bowling"),                        // 1 byte
            (426, 1, "crosscountryskiing_snowshoeing"), // 1 byte
            (427, 1, "jogging_running"),                // 1 byte
            (428, 1, "racquet_sports"),                 // 1 byte
            (429, 1, "yoga_pilates_martial_arts_2016"), // 1 byte
            (430, 1, "bottled_water_2016"),             // 1 byte
            (431, 1, "regular_soft_drinks_2016"),       // 1 byte
            (432, 1, "diet_soft_drinks_2016"),          // 1 byte
            (433, 1, "sports_drinks_2016"),             // 1 byte
            (434, 1, "energy_drinks_2016"),             // 1 byte
            (435, 1, "juice_2016"),                     // 1 byte
            (436, 1, "coffee__2016"),                   // 1 byte
            (437, 1, "tea_2016"),                       // 1 byte
            (438, 1, "milk_2016"),                      // 1 byte
            (439, 1, "beer_2016"),                      // 1 byte
            (440, 1, "wine_2016"),                      // 1 byte
            (441, 1, "coolers_2016"),                   // 1 byte
            (442, 1, "spirits_liquor_2016"),            // 1 byte
            (443, 1, "smart_tv_in_household"),          // 1 byte
            (444, 1, "oil_change_rt.store_auto_centr"), // 1 byte
            (445, 1, "tune_up_rt.store_auto_centr"),    // 1 byte
            (446, 1, "replmufflr_rt.store_auto_centr"), // 1 byte
            (447, 1, "rep.brakes_rt.store_auto_centr"), // 1 byte
            (448, 1, "repl_chg_tre_rt.storeautcentr"),  // 1 byte
            (449, 1, "rpl_rpr_wdshd_rt.storeautcentr"), // 1 byte
            (450, 1, "autobdy_repr_specautocentre"),    // 1 byte
            (451, 1, "small_app_past_12_mos_2020"),     // 1 byte
            (452, 1, "large_app__past_12_mos_2020"),    // 1 byte
            (453, 1, "compeqp_accs_past_12m_2020"),     // 1 byte
            (454, 1, "comp_sofw_gams_past_12m_2020"),   // 1 byte
            (455, 1, "electronics_spdg_past_12m_2020"), // 1 byte
            (456, 1, "furniture_spent_past_12m_2020"),  // 1 byte
            (457, 1, "vidgamssys_games_past_12m_2020"), // 1 byte
            (458, 1, "smart_tv_connected_to_internet"), // 1 byte
            (459, 1, "coffee_donut_shop"),              // 1 byte
            (460, 1, "fast_food_restaurant"),           // 1 byte
            (461, 1, "casual_family_dining_restauran"), // 1 byte
            (462, 1, "fine_dining_restaurant"),         // 1 byte
            (463, 1, "bar_pub"),                        // 1 byte
            (464, 1, "smart_speaker"),                  // 1 byte
            (465, 1, "use_smart_tv"),                   // 1 byte
            (466, 1, "game_console"),                   // 1 byte
            (467, 1, "access_radio_stn_website"),       // 1 byte
            (468, 1, "access_tv_stn_website"),          // 1 byte
            (469, 1, "listen_subs_music_service"),      // 1 byte
            (470, 1, "listen_rd_stream_app"),           // 1 byte
            (471, 1, "watch_tv_stream_app"),            // 1 byte
            (472, 1, "watch_other_video"),              // 1 byte
            (473, 1, "research_prods_services"),        // 1 byte
            (474, 1, "purchase_prods_services"),        // 1 byte
            (475, 1, "download_or_play_podcasts"),      // 1 byte
            (476, 1, "visit_a_social_networking_site"), // 1 byte
            (477, 1, "post_share_content,_blog"),       // 1 byte
            (478, 1, "banking"),                        // 1 byte
            (479, 1, "enter_online_contests"),          // 1 byte
            (480, 1, "register_webs_newsltr"),          // 1 byte
            (481, 1, "redeem_online_offer"),            // 1 byte
            (482, 1, "play_video_games_online"),        // 1 byte
            (489, 1, "business_travel_past_12_months"), // 1 byte
            (490, 1, "daily_newspaper_past_7_days"),    // 1 byte
            (491, 1, "weekend_newspaper_past_7_days"),  // 1 byte
            (492, 1, "community_newspaper_pst_7_days"), // 1 byte
            (493, 1, "magazine_past_7_days"),           // 1 byte
            (494, 1, "childrens_clth_past_12m_2020"),   // 1 byte
            (495, 1, "mens_clth_past_12m_2020"),        // 1 byte
            (496, 1, "womens_clth_past_12m_2020"),      // 1 byte
            (497, 1, "cosmetics_past_12_mos_2020"),     // 1 byte
            (498, 1, "sporting_goods_past_12_m_2020"),  // 1 byte
            (501, 1, "gender"),                         // 1 byte
            (502, 1, "internet_hours_previous_day")    // 1 byte

        };

        public static readonly List<(int Position, int Length, string Header)> AuSwdColumns = new List<(int, int, string)>
        {
            (0,  7, "household_id"),                      // First 7 bytes
            (7,  2, "member_id"),                         // 2 bytes
            (9,  4, "station_code"),                      // 4 bytes
            (13, 6, "start_time"),                        // 6 bytes
            (19, 6, "end_time"),                          // 6 bytes
            (25, 1, "location_id"),                       // 1 byte
            (26, 1, "activity_type"),                     // 1 byte
            (27, 1, "reserved")                           // 1 byte
        };

        public static readonly List<(int Position, int Length, string Header)> PbSwdColumns = new List<(int, int, string)>
        {
            (0,  7, "household_id"),                      // First 7 bytes
            (7,  2, "member_id"),                         // 2 bytes
            (9,  4, "station_code"),                      // 4 bytes
            (13, 6, "start_time"),                        // 6 bytes
            (19, 6, "end_time"),                          // 6 bytes
            (25, 1, "location_id"),                       // 1 byte
            (26, 1, "activity_type"),                     // 1 byte
            (27, 1, "reserved"),                          // 1 byte
            (28, 8, "recording_date")                     // 8 byte
        };

        public static readonly List<(int Position, int Length, string Header)> StColumns = new List<(int, int, string)>
        {
            (0,  4, "station_code"),                      // First 4 bytes
            (5,  1, "time_zone")                          // 1 byte
        };
    }
}
