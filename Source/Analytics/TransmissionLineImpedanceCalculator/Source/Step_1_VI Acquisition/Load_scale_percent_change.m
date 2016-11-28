%% Change the scale of the load 
percent = load('load_alter_percent_sample_origin.mat');
load_alter_percent_sample = percent.load_alter_percent_sample;

range = 21;

center = mean(load_alter_percent_sample,1);

for idx = 1 : size(load_alter_percent_sample,1)
    
    load_alter_percent_sample(idx,1) = center(1,1) + range*(load_alter_percent_sample(idx,1) - center(1,1));
    load_alter_percent_sample(idx,2) = center(1,2) + range*(load_alter_percent_sample(idx,2) - center(1,2));
    
end

save('load_alter_percent_sample.mat','load_alter_percent_sample');