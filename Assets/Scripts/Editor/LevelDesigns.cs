using System.Collections.Generic;

namespace Oath.EditorTools
{
    /// <summary>Nội dung thiết kế 5 màn chiến dịch: cốt truyện, bản đồ, kinh tế và các đợt tấn công.</summary>
    public static class LevelDesigns
    {
        static DialogLine L(string who, string text) => new DialogLine(who, text);
        static SpawnGroup G(string id, int n, int sp = 0, float delay = 0f, float interval = 1.3f) => new SpawnGroup(id, n, sp, delay, interval);
        static WaveConfig W(string name, int bonus, params SpawnGroup[] g) => new WaveConfig { name = name, bonusGold = bonus, groups = new List<SpawnGroup>(g) };

        /// <summary>Cấu hình chế độ Sinh tồn: các đợt được sinh tự động trong WaveManager.</summary>
        public static void FillEndless(LevelConfig c)
        {
            Fill(c, 4);
            c.levelIndex = 0;
            c.sceneName = SceneLoader.Endless;
            c.endless = true;
            c.title = "Vòng Vây Bất Tận";
            c.location = "Kinh đô Elaria";
            c.description = "Các đợt địch nối tiếp không ngừng. Trụ càng lâu càng tốt!";
            c.objective = "Sinh tồn";
            c.map = LevelMaps.L4;
            c.groundColor = 2;
            c.startWood = 320; c.startGold = 320; c.castleHp = 2500;
            c.startPawns = 3; c.basePopulation = 14;
            c.firstWavePrep = 45f; c.betweenWavePrep = 20f;
            c.waves = new List<WaveConfig>();
            c.intro = new List<DialogLine>
            {
                L("Mira", "Trinh sát báo: lũ quái vật kéo tới từ khắp các hòn đảo, không biết bao giờ mới dứt."),
                L("Kael", "Vậy thì chúng ta sẽ đứng vững tới cùng. Xây tháp, giữ kinh đô — trụ được bao lâu hay bấy lâu!"),
            };
            c.outro = new List<DialogLine>();
            c.tutorialHints = new List<string>
            {
                "Chế độ Sinh tồn: mỗi đợt mạnh hơn đợt trước, cứ <b>5 đợt</b> lại có một thủ lĩnh.",
                "Kết hợp nhiều loại tháp: Bắn Đá cho đám đông, Bi Sắt cho quái giáp dày, Búa Địa Chấn giữ chân địch.",
            };
        }

        public static void Fill(LevelConfig c, int level)
        {
            c.levelIndex = level;
            c.sceneName = SceneLoader.LevelScene(level);
            c.intro = new List<DialogLine>();
            c.outro = new List<DialogLine>();
            c.tutorialHints = new List<string>();
            c.waves = new List<WaveConfig>();
            c.endless = false;
            c.towers = new List<TowerType> { TowerType.Archer, TowerType.Catapult, TowerType.Iron, TowerType.Fire, TowerType.Quake };
            c.roadColor = 4;
            c.protectVillage = false;
            c.allowTowerUpgrade = true;
            c.maxTowerLevel = 3;
            c.startPawns = 2;
            c.basePopulation = 10;

            switch (level)
            {
                case 1:
                    c.title = "Ngọn Lửa Đầu Tiên";
                    c.location = "Làng Greenhaven";
                    c.description = "Quân Cờ Đỏ đổ bộ lên bờ đông Greenhaven. Hãy làm quen với việc xây tháp, tuyển quân và điều quân để giữ lâu đài.";
                    c.objective = "Bảo vệ lâu đài";
                    c.map = LevelMaps.L1;
                    c.groundColor = 1;
                    c.startWood = 200; c.startGold = 180; c.castleHp = 1500; c.startPawns = 3;
                    c.firstWavePrep = 50f; c.betweenWavePrep = 25f;
                    c.allowTowerUpgrade = true; c.maxTowerLevel = 2;
                    c.intro.Add(L("Kể chuyện", "Quần đảo Elaria. Ở trung tâm là Trái Tim Bình Minh — tinh thể cổ đại giữ cân bằng cho muôn đảo. Nhưng triều đình đã dồn gần hết năng lượng về kinh đô, bỏ mặc các đảo xa."));
                    c.intro.Add(L("Kể chuyện", "Tướng Veyr, người từng phục vụ vương triều, đã tập hợp quân Cờ Đỏ. Sáng nay, những cánh buồm đỏ xuất hiện ngoài khơi làng Greenhaven."));
                    c.intro.Add(L("Mira", "Đội trưởng Kael! Thuyền địch đã cập bờ phía đông. Chúng sẽ theo con đường mòn tiến thẳng tới lâu đài."));
                    c.intro.Add(L("Kael", "Dân làng còn chưa kịp sơ tán. Chúng ta phải giữ lâu đài qua ba đợt tấn công."));
                    c.intro.Add(L("Mira", "Các chấm đỏ trên bản đồ là đường tiến quân của chúng. Xây tháp canh dọc đường, tuyển lính ở doanh trại, và chú ý lối hẹp giữa hai mỏm đá!"));
                    c.outro.Add(L("Kael", "Greenhaven vẫn đứng vững. Nhưng đây mới chỉ là đội tiên phong."));
                    c.outro.Add(L("Mira", "Tù binh khai rằng Veyr đang tiến về Cây Cầu Đá — con đường duy nhất nối sang đảo trong. Chúng ta phải tới đó trước hắn."));
                    c.tutorialHints.Add("Chạm vào một <b>ô xây dựng</b> (biểu tượng búa) rồi chọn <b>Tháp canh</b>.");
                    c.tutorialHints.Add("Chạm <b>Doanh trại</b> để tuyển Kiếm sĩ hoặc Cung thủ.");
                    c.tutorialHints.Add("Chạm một người lính (hoặc nút <b>Chọn cả đội</b>), rồi chạm lên bản đồ để di chuyển tới lối hẹp.");
                    c.tutorialHints.Add("<b>Thợ</b> tự chặt gỗ và đào vàng. Chạm <b>Lâu đài</b> để tuyển thêm Thợ.");
                    c.tutorialHints.Add("Sẵn sàng? Nhấn <b>Gọi ngay</b> để địch tới sớm và nhận thêm vàng.");
                    c.tutorialHints.Add("Mới: <b>Tháp Bắn Đá</b> ném đá gây sát thương lan — rất hợp để chặn đám đông ở lối hẹp.");
                    c.towers = new List<TowerType> { TowerType.Archer, TowerType.Catapult };
                    c.waves.Add(W("Đội tiên phong", 40, G("red_warrior", 4, 0, 0, 2.2f), G("goblin", 3, 0, 7, 1.6f)));
                    c.waves.Add(W("Cung thủ yểm trợ", 50, G("red_warrior", 5, 0, 0, 1.8f), G("red_archer", 3, 0, 5, 2f), G("goblin", 4, 0, 11, 1.2f)));
                    c.waves.Add(W("Tổng tấn công", 60, G("red_warrior", 6, 0, 0, 1.5f), G("goblin_warrior", 3, 0, 5, 2f), G("red_archer", 4, 0, 9, 1.6f), G("goblin", 6, 0, 15, 0.8f)));
                    break;

                case 2:
                    c.title = "Cây Cầu Gãy";
                    c.location = "Cây Cầu Đá";
                    c.description = "Dải đất hẹp như cây cầu đá là lối duy nhất nối hai đảo. Giữ cây cầu, đừng để địch tràn sang bờ tây.";
                    c.objective = "Giữ cây cầu đá";
                    c.map = LevelMaps.L2;
                    c.groundColor = 2;
                    c.startWood = 180; c.startGold = 230; c.castleHp = 1600;
                    c.firstWavePrep = 45f; c.betweenWavePrep = 25f;
                    c.maxTowerLevel = 2;
                    c.intro.Add(L("Mira", "Cây cầu phía bắc đã bị phá sập. Giờ dải đất hẹp này là con đường duy nhất sang bờ tây."));
                    c.intro.Add(L("Kael", "Lối càng hẹp, phòng tuyến càng chắc. Thương binh sẽ đứng chặn đầu cầu, cung thủ yểm trợ phía sau."));
                    c.intro.Add(L("Veyr", "Đội trưởng của triều đình… Ngươi giữ được một cây cầu, nhưng giữ được cả quần đảo đang mục ruỗng sao?"));
                    c.intro.Add(L("Mira", "Hắn còn dẫn theo lũ quái vật từ hang động — chất nhờn và bộ xương. Cẩn thận Vua Chất Nhờn, khi chết nó tách thành nhiều con nhỏ!"));
                    c.outro.Add(L("Kael", "Cây cầu vẫn trong tay chúng ta. Nhưng vì sao quái vật lại nghe lệnh Cờ Đỏ?"));
                    c.outro.Add(L("Mira", "Những hòn đảo bị lãng quên ở phía nam… năng lượng tinh thể ở đó cạn kiệt, sinh vật trở nên điên loạn. Dân làng ở đó đang gặp nguy."));
                    c.tutorialHints.Add("Mẹo: đặt <b>Thương binh</b> ở đầu cầu — chúng rất trâu và làm chậm kẻ địch.");
                    c.tutorialHints.Add("Tháp có thể <b>nâng cấp</b> để bắn mạnh và xa hơn.");
                    c.tutorialHints.Add("Mới: <b>Tháp Bi Sắt Gai</b> xuyên giáp và làm chậm — khắc chế Chất nhờn đá và Xương khiên.");
                    c.towers = new List<TowerType> { TowerType.Archer, TowerType.Catapult, TowerType.Iron };
                    c.waves.Add(W("Lũ chất nhờn", 45, G("slime", 6, 0, 0, 1.4f), G("goblin", 4, 0, 8, 1.2f)));
                    c.waves.Add(W("Đạo quân xương", 55, G("skull", 6, 0, 0, 1.4f), G("skull_archer", 3, 0, 6, 2f), G("red_lancer", 2, 0, 12, 3f)));
                    c.waves.Add(W("Tường khiên", 65, G("slime_stone", 3, 0, 0, 2.5f), G("red_lancer", 4, 0, 4, 2f), G("red_warrior", 5, 0, 10, 1.4f), G("goblin_archer", 4, 0, 14, 1.6f)));
                    c.waves.Add(W("Vua Chất Nhờn", 80, G("skull_warrior", 4, 0, 0, 2f), G("skull", 8, 0, 6, 1f), G("red_archer", 4, 0, 10, 1.6f), G("slime_king", 1, 0, 22, 1f)));
                    break;

                case 3:
                    c.title = "Những Hòn Đảo Bị Lãng Quên";
                    c.location = "Làng chài phía nam";
                    c.description = "Địch tấn công từ hai hướng. Phân bổ tài nguyên, sửa chữa công trình và bảo vệ nhà dân làng.";
                    c.objective = "Bảo vệ lâu đài và dân làng";
                    c.map = LevelMaps.L3;
                    c.groundColor = 3;
                    c.startWood = 230; c.startGold = 240; c.castleHp = 1700;
                    c.startPawns = 3;
                    c.firstWavePrep = 50f; c.betweenWavePrep = 25f;
                    c.protectVillage = true; c.villageMinAlive = 1;
                    c.intro.Add(L("Dân làng", "Lạy trời, quân Lam đã tới! Bao năm nay chẳng ai từ kinh đô ngó ngàng tới làng chúng tôi…"));
                    c.intro.Add(L("Kael", "Ta xin lỗi vì đã tới muộn. Lần này chúng ta sẽ không bỏ ai lại."));
                    c.intro.Add(L("Mira", "Trinh sát báo địch tiến từ hai phía: bờ tây và bờ đông. Con đường phía tây đi ngang qua làng!"));
                    c.intro.Add(L("Mira", "Nếu tất cả nhà dân bị phá hủy, nhiệm vụ thất bại. Hãy dùng gỗ để sửa công trình khi không có địch ở gần."));
                    c.outro.Add(L("Dân làng", "Cảm ơn các anh. Tướng Veyr… ông ấy sinh ra ở làng bên cạnh. Năm đói kém, kinh đô đã khóa kho lương."));
                    c.outro.Add(L("Kael", "Vậy ra đó là lý do hắn phản lại triều đình. Nhưng chiến tranh chỉ khiến người dân khổ thêm."));
                    c.outro.Add(L("Mira", "Kael, tin khẩn! Veyr dồn toàn bộ lực lượng vây hãm kinh đô. Hắn muốn chiếm Trái Tim Bình Minh."));
                    c.tutorialHints.Add("Chú ý <b>nhà dân</b> phía tây bắc — chạm vào để sửa chữa khi bị hư hại.");
                    c.tutorialHints.Add("<b>Bom Biết Đi</b> nổ tung khi chạm mục tiêu — hãy hạ chúng từ xa!");
                    c.tutorialHints.Add("Mới: <b>Tháp Hỏa Ngục</b> thiêu đốt cả đám kẻ địch theo thời gian.");
                    c.towers = new List<TowerType> { TowerType.Archer, TowerType.Catapult, TowerType.Iron, TowerType.Fire };
                    c.waves.Add(W("Hai mũi tấn công", 50, G("duck", 5, 0, 0, 1.6f), G("goblin", 5, 1, 3, 1.4f)));
                    c.waves.Add(W("Nấm độc", 55, G("mushroom", 3, 0, 0, 2.5f), G("red_warrior", 5, 1, 2, 1.6f), G("eggplant", 2, 0, 8, 2.5f)));
                    c.waves.Add(W("Bom!", 65, G("bomb", 3, 1, 0, 3f), G("boar", 3, 0, 2, 2.2f), G("duck", 6, 1, 8, 1.2f), G("goblin_green_archer", 4, 0, 10, 1.6f)));
                    c.waves.Add(W("Bầy đàn", 75, G("boar", 4, 1, 0, 2f), G("mushroom", 4, 0, 0, 2.2f), G("eggplant", 4, 1, 8, 2f), G("spiderling", 6, 0, 10, 1.2f), G("bomb", 3, 0, 16, 2.5f)));
                    c.waves.Add(W("Đầu đàn", 90, G("boar_elite", 2, 0, 0, 4f), G("duck_elite", 6, 1, 2, 1.3f), G("mushroom_elite", 3, 1, 10, 2.5f), G("bomb", 4, 0, 12, 2f), G("red_lancer", 4, 1, 16, 1.8f), G("red_monk", 2, 0, 18, 3f)));
                    break;

                case 4:
                    c.title = "Cuộc Vây Hãm Kinh Đô";
                    c.location = "Kinh đô Elaria";
                    c.description = "Kinh đô bị bao vây từ ba hướng. Nâng cấp tháp, quản lý thời gian giữa các đợt và đối mặt với quái thú huyền thoại.";
                    c.objective = "Giữ vững kinh đô";
                    c.map = LevelMaps.L4;
                    c.groundColor = 1;
                    c.startWood = 280; c.startGold = 300; c.castleHp = 2200;
                    c.startPawns = 3; c.basePopulation = 14;
                    c.firstWavePrep = 55f; c.betweenWavePrep = 28f;
                    c.intro.Add(L("Lính gác", "Báo cáo! Địch xuất hiện ở bến bắc, bờ đông và bến nam. Chúng ta bị bao vây!"));
                    c.intro.Add(L("Veyr", "Kinh đô hưởng ánh sáng của tinh thể suốt trăm năm trong khi các đảo xa chìm trong bóng tối. Hôm nay, ánh sáng sẽ đổi chủ."));
                    c.intro.Add(L("Kael", "Ánh sáng không thuộc về riêng ai, Veyr. Nhưng ta sẽ không để ngươi đốt trụi kinh đô."));
                    c.intro.Add(L("Mira", "Hắn còn triệu hồi những quái thú cổ xưa: sói, ngựa địa ngục, nhân mã… và cả Sư Tử Nemea. Nâng cấp tháp lên tối đa!"));
                    c.outro.Add(L("Kael", "Kinh đô vẫn đứng vững… nhưng Trái Tim Bình Minh đang rung chuyển."));
                    c.outro.Add(L("Mira", "Trận chiến đã làm tinh thể mất ổn định. Veyr đang dẫn đội quân tinh nhuệ cuối cùng tới. Đây sẽ là trận quyết định."));
                    c.tutorialHints.Add("Địch tới từ <b>ba hướng</b>. Hai tháp canh có sẵn bên cạnh lâu đài — hãy nâng cấp chúng.");
                    c.tutorialHints.Add("Dùng <b>Tu sĩ</b> để hồi máu cho đội quân trong trận dài.");
                    c.tutorialHints.Add("Mới: <b>Tháp Búa Địa Chấn</b> gây choáng mọi kẻ địch quanh tháp. <b>Quỷ Fury</b> biết bay — chỉ Tháp Cung, Bi Sắt và Hỏa Ngục bắn được.");
                    c.waves.Add(W("Bầy sói", 60, G("wolf", 6, 0, 0, 1.2f), G("red_warrior", 6, 1, 3, 1.5f)));
                    c.waves.Add(W("Xương trắng", 65, G("skull", 8, 2, 0, 1.1f), G("skull_archer", 4, 1, 4, 1.8f), G("wolf", 5, 0, 8, 1.2f)));
                    c.waves.Add(W("Kỵ binh địa ngục", 75, G("hellhorse", 3, 1, 0, 3f), G("centaur", 2, 0, 4, 3f), G("red_lancer", 5, 2, 2, 1.8f), G("red_archer", 5, 1, 10, 1.5f)));
                    c.waves.Add(W("Mạng nhện", 85, G("arachne", 2, 2, 0, 5f), G("fury", 3, 0, 4, 2.5f), G("goblin_green_warrior", 6, 1, 2, 1.5f), G("wolf", 8, 2, 12, 0.9f)));
                    c.waves.Add(W("Vòng vây siết chặt", 95, G("hellhorse", 3, 0, 0, 3f), G("centaur", 2, 1, 4, 3f), G("skull_poison_warrior", 4, 2, 2, 2.2f), G("fury", 2, 1, 12, 3f), G("red_monk", 2, 0, 10, 3f)));
                    c.waves.Add(W("Sư Tử Nemea", 120, G("wolf", 8, 0, 0, 1f), G("arachne", 1, 2, 4, 5f), G("centaur", 2, 1, 6, 3f), G("hellhorse", 2, 2, 12, 3f), G("lion", 1, 1, 18, 1f)));
                    break;

                default:
                    c.title = "Ánh Sáng Thuộc Về Tất Cả";
                    c.location = "Quảng trường Bình Minh";
                    c.description = "Trận chiến cuối cùng chống đội quân tinh nhuệ của Veyr và quái thú Chimera. Giữ vững trước khi Trái Tim Bình Minh sụp đổ.";
                    c.objective = "Đánh bại đội quân của Veyr";
                    c.map = LevelMaps.L5;
                    c.groundColor = 3;
                    c.startWood = 360; c.startGold = 360; c.castleHp = 3000;
                    c.startPawns = 4; c.basePopulation = 16;
                    c.firstWavePrep = 55f; c.betweenWavePrep = 28f;
                    c.intro.Add(L("Kể chuyện", "Trái Tim Bình Minh chập chờn. Những vết nứt ánh sáng lan khắp quảng trường."));
                    c.intro.Add(L("Veyr", "Kael! Hãy nhìn đi — tinh thể sắp vỡ. Nếu ta không chiếm được nó, không ai được hưởng cả!"));
                    c.intro.Add(L("Kael", "Nếu nó vỡ, cả quần đảo sẽ chìm vào bóng tối, kể cả quê hương của ông. Ta sẽ chặn ông lại, rồi chúng ta sẽ cùng sửa chữa sai lầm này."));
                    c.intro.Add(L("Mira", "Ba hướng tấn công, và trinh sát thấy bóng một con Chimera khổng lồ. Đây là trận cuối cùng — dốc toàn lực!"));
                    c.outro.Add(L("Veyr", "Ta… đã thua. Nhưng tinh thể…"));
                    c.outro.Add(L("Kael", "Chưa quá muộn. Hãy giúp chúng ta kích hoạt lại các điểm dẫn truyền."));
                    c.tutorialHints.Add("<b>Tướng Veyr</b> và <b>Chimera</b> xuất hiện ở đợt cuối — tập trung toàn quân!");
                    c.tutorialHints.Add("Đừng quên tuyển Thợ và xây Nhà ở để mở rộng quân số.");
                    c.waves.Add(W("Tiền quân Cờ Đỏ", 70, G("red_warrior", 8, 0, 0, 1.3f), G("red_archer", 5, 1, 3, 1.6f), G("goblin_green", 8, 2, 6, 0.9f)));
                    c.waves.Add(W("Lửa và độc", 80, G("slime_fire", 8, 1, 0, 1.1f), G("slime_fire_stone", 3, 0, 3, 2.5f), G("skull_poison", 6, 2, 4, 1.3f), G("skull_poison_archer", 4, 0, 10, 1.8f)));
                    c.waves.Add(W("Quân tạp nham", 90, G("boar_elite", 2, 0, 0, 3f), G("duck_elite", 5, 1, 2, 1.3f), G("bomb", 3, 2, 6, 2.5f), G("eggplant", 3, 1, 10, 2f), G("red_lancer", 4, 0, 12, 1.8f)));
                    c.waves.Add(W("Quái thú cổ xưa", 100, G("fury", 3, 2, 0, 2.5f), G("centaur", 3, 0, 3, 2.8f), G("arachne", 1, 1, 6, 5f), G("hellhorse", 3, 1, 12, 2.5f), G("slime_fire_king", 1, 0, 16, 1f)));
                    c.waves.Add(W("Bức tường thép", 110, G("lion", 1, 0, 10, 1f), G("red_lancer", 6, 1, 0, 1.6f), G("red_monk", 3, 1, 6, 2.8f), G("skull_poison_warrior", 4, 2, 2, 2f), G("wolf", 8, 0, 4, 0.9f)));
                    c.waves.Add(W("Đội quân tinh nhuệ của Veyr", 0, G("red_warrior", 8, 0, 0, 1.2f), G("red_archer", 5, 1, 2, 1.5f), G("red_lancer", 4, 2, 4, 1.8f), G("hellhorse", 3, 1, 10, 2.2f), G("fury", 2, 2, 12, 3f), G("veyr", 1, 0, 12, 1f), G("chimera", 1, 1, 26, 1f)));
                    break;
            }
        }
    }
}
