namespace Thanks.Fantasy
{
    public partial class HeightmapGenerator
    {
        // Heighmap Template: Volcano
        private void templateVolcano()
        {
            addStep("Hill", "1", "90-100", "44-56", "40-60");
            addStep("Multiply", .8, "50-100");
            addStep("Range", "1.5", "30-55", "45-55", "40-60");
            addStep("Smooth", 2);
            addStep("Hill", "1.5", "25-35", "25-30", "20-75");
            addStep("Hill", "1", "25-35", "75-80", "25-75");
            addStep("Hill", "0.5", "20-25", "10-15", "20-25");
        }

        // Heighmap Template: High Island
        private void templateHighIsland()
        {
            addStep("Hill", "1", "90-100", "65-75", "47-53");
            addStep("Add", 5, "all");
            addStep("Hill", "6", "20-23", "25-55", "45-55");
            addStep("Range", "1", "40-50", "45-55", "45-55");
            addStep("Smooth", 2);
            addStep("Trough", "2-3", "20-30", "20-30", "20-30");
            addStep("Trough", "2-3", "20-30", "60-80", "70-80");
            addStep("Hill", "1", "10-15", "60-60", "50-50");
            addStep("Hill", "1.5", "13-16", "15-20", "20-75");
            addStep("Multiply", .8, "20-100");
            addStep("Range", "1.5", "30-40", "15-85", "30-40");
            addStep("Range", "1.5", "30-40", "15-85", "60-70");
            addStep("Pit", "2-3", "10-15", "15-85", "20-80");
        }

        // Heighmap Template: Low Island
        private void templateLowIsland()
        {
            addStep("Hill", "1", "90-99", "60-80", "45-55");
            addStep("Hill", "4-5", "25-35", "20-65", "40-60");
            addStep("Range", "1", "40-50", "45-55", "45-55");
            addStep("Smooth", 3);
            addStep("Trough", "1.5", "20-30", "15-85", "20-30");
            addStep("Trough", "1.5", "20-30", "15-85", "70-80");
            addStep("Hill", "1.5", "10-15", "5-15", "20-80");
            addStep("Hill", "1", "10-15", "85-95", "70-80");
            addStep("Pit", "3-5", "10-15", "15-85", "20-80");
            addStep("Multiply", .4, "20-100");
        }

        // Heighmap Template: Continents
        private void templateContinents()
        {
            addStep("Hill", "1", "80-85", "75-80", "40-60");
            addStep("Hill", "1", "80-85", "20-25", "40-60");
            addStep("Multiply", .22, "20-100");
            addStep("Hill", "5-6", "15-20", "25-75", "20-82");
            addStep("Range", ".8", "30-60", "5-15", "20-45");
            addStep("Range", ".8", "30-60", "5-15", "55-80");
            addStep("Range", "0-3", "30-60", "80-90", "20-80");
            addStep("Trough", "3-4", "15-20", "15-85", "20-80");
            addStep("Strait", "2", "vertical");
            addStep("Smooth", 2);
            addStep("Trough", "1-2", "5-10", "45-55", "45-55");
            addStep("Pit", "3-4", "10-15", "15-85", "20-80");
            addStep("Hill", "1", "5-10", "40-60", "40-60");
        }

        // Heighmap Template: Archipelago
        private void templateArchipelago()
        {
            addStep("Add", 11, "all");
            addStep("Range", "2-3", "40-60", "20-80", "20-80");
            addStep("Hill", "5", "15-20", "10-90", "30-70");
            addStep("Hill", "2", "10-15", "10-30", "20-80");
            addStep("Hill", "2", "10-15", "60-90", "20-80");
            addStep("Smooth", 3);
            addStep("Trough", "10", "20-30", "5-95", "5-95");
            addStep("Strait", "2", "vertical");
            addStep("Strait", "2", "horizontal");
        }

        // Heighmap Template: Atoll
        private void templateAtoll()
        {
            addStep("Hill", "1", "75-80", "50-60", "45-55");
            addStep("Hill", "1.5", "30-50", "25-75", "30-70");
            addStep("Hill", ".5", "30-50", "25-35", "30-70");
            addStep("Smooth", 1);
            addStep("Multiply", .2, "25-100");
            addStep("Hill", ".5", "10-20", "50-55", "48-52");
        }

        // Heighmap Template: Mediterranean
        private void templateMediterranean()
        {
            addStep("Range", "3-4", "30-50", "0-100", "0-10");
            addStep("Range", "3-4", "30-50", "0-100", "90-100");
            addStep("Hill", "5-6", "30-70", "0-100", "0-5");
            addStep("Hill", "5-6", "30-70", "0-100", "95-100");
            addStep("Smooth", 1);
            addStep("Hill", "2-3", "30-70", "0-5", "20-80");
            addStep("Hill", "2-3", "30-70", "95-100", "20-80");
            addStep("Multiply", .8, "land");
            addStep("Trough", "3-5", "40-50", "0-100", "0-10");
            addStep("Trough", "3-5", "40-50", "0-100", "90-100");
        }

        // Heighmap Template: Peninsula
        private void templatePeninsula()
        {
            addStep("Range", "2-3", "20-35", "40-50", "0-15");
            addStep("Add", 5, "all");
            addStep("Hill", "1", "90-100", "10-90", "0-5");
            addStep("Add", 13, "all");
            addStep("Hill", "3-4", "3-5", "5-95", "80-100");
            addStep("Hill", "1-2", "3-5", "5-95", "40-60");
            addStep("Trough", "5-6", "10-25", "5-95", "5-95");
            addStep("Smooth", 3);
        }

        // Heighmap Template: Pangea
        private void templatePangea()
        {
            addStep("Hill", "1-2", "25-40", "15-50", "0-10");
            addStep("Hill", "1-2", "5-40", "50-85", "0-10");
            addStep("Hill", "1-2", "25-40", "50-85", "90-100");
            addStep("Hill", "1-2", "5-40", "15-50", "90-100");
            addStep("Hill", "8-12", "20-40", "20-80", "48-52");
            addStep("Smooth", 2);
            addStep("Multiply", .7, "land");
            addStep("Trough", "3-4", "25-35", "5-95", "10-20");
            addStep("Trough", "3-4", "25-35", "5-95", "80-90");
            addStep("Range", "5-6", "30-40", "10-90", "35-65");
        }

        // Heighmap Template: Isthmus
        private void templateIsthmus()
        {
            addStep("Hill", "5-10", "15-30", "0-30", "0-20");
            addStep("Hill", "5-10", "15-30", "10-50", "20-40");
            addStep("Hill", "5-10", "15-30", "30-70", "40-60");
            addStep("Hill", "5-10", "15-30", "50-90", "60-80");
            addStep("Hill", "5-10", "15-30", "70-100", "80-100");
            addStep("Smooth", 2);
            addStep("Trough", "4-8", "15-30", "0-30", "0-20");
            addStep("Trough", "4-8", "15-30", "10-50", "20-40");
            addStep("Trough", "4-8", "15-30", "30-70", "40-60");
            addStep("Trough", "4-8", "15-30", "50-90", "60-80");
            addStep("Trough", "4-8", "15-30", "70-100", "80-100");
        }

        // Heighmap Template: Shattered
        private void templateShattered()
        {
            addStep("Hill", "8", "35-40", "15-85", "30-70");
            addStep("Trough", "10-20", "40-50", "5-95", "5-95");
            addStep("Range", "5-7", "30-40", "10-90", "20-80");
            addStep("Pit", "12-20", "30-40", "15-85", "20-80");
        }
    }
}