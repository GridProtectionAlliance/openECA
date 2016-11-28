function [KI_injection_set]=single_bus_injections_ratio_errors_estimation(current_bus_number,injection_line_number_set,...
    accurate_line_number,KI_accurate,calibration_history,line_estimation_results)
%% Check injection valid
if ~find(injection_line_number_set(:,1)==accurate_line_number) % check if the set include the accurate line
    disp('Error. No accurate line. Estimation failed.');
end

%% Form the KIs estimation
inaccurate_line_num = size(injection_line_number_set,1)-1;
inaccurate_injection_lines_set = [];
KI_injection_set = [];% [line_number KI]

% I_leftside_set, I_rightside_set 
I_leftside_set=[];
I_rightside_set=[];
for j=1:(size(injection_line_number_set,1))
    current_line_number = injection_line_number_set(j,1);
    
    temp_line_name=['line_' ,num2str(current_line_number), '_measured_positive_sequence.mat'];
    temp_all_data_struct = load(temp_line_name);
    temp_all_data = temp_all_data_struct.VI_measurement_set;

%     temp_line_name=['line_' ,num2str(current_line_number), '_true_positive_sequence.mat'];
%     temp_all_data_struct = load(temp_line_name);
%     temp_all_data = temp_all_data_struct.VI_true_set;

    if injection_line_number_set(j,2) == 0
        I_complex_temp = temp_all_data(:,2);
    else
        I_complex_temp = temp_all_data(:,4);
    end
    
    if current_line_number == accurate_line_number
        I_complex_temp = I_complex_temp*KI_accurate;
        I_rightside_set = [I_rightside_set, I_complex_temp];        
    else
        if ~isempty(calibration_history) && sum(find(calibration_history(:,1)==current_line_number)) 
        % if curent line has been calibrated, move it to the right hand side. And remove it from the inaccurate lines
            temp_index = find(line_estimation_results(:,1)==current_line_number); 
            if line_estimation_results(temp_index,2)==current_bus_number
                temp_KI = line_estimation_results(temp_index,4);
            else
                temp_KI = line_estimation_results(temp_index,7);
            end
            I_rightside_set = [I_rightside_set, temp_KI*I_complex_temp];
        else
            I_leftside_set = [I_leftside_set, I_complex_temp];
            inaccurate_injection_lines_set = [inaccurate_injection_lines_set; current_line_number];
        end
    end
end

I_rightside_set = sum(I_rightside_set,2);

% find columns have similar currents with right hand side
idx2_record = [];
temp_line_set = inaccurate_injection_lines_set(:,1);
current_leftside_columns = size(I_leftside_set,2);
idx2 = 1;
while idx2 <= current_leftside_columns
    current_set_1 = I_leftside_set(:,idx2);
    current_set_2 = I_rightside_set;
    difference = abs(mean(current_set_1-current_set_2));
    
    if difference<=5
        current_KI_set = current_set_2./current_set_1;
        current_KI = mean(current_KI_set);
        
        KI_injection_set = [KI_injection_set; temp_line_set(idx2,1), current_KI];
        I_rightside_set = 2*I_rightside_set;
        
        left_columns = 1:size(I_leftside_set,2);
        I_leftside_set = I_leftside_set(:,left_columns(1,:)~=idx2);
        temp_line_set = temp_line_set(left_columns(1,:)~=idx2,1);
        current_leftside_columns = size(I_leftside_set,2);
        
        idx2_record = [idx2_record; idx2];
        idx2=1; 
    else
        idx2 = idx2 + 1;
    end 
end

% find all samilar currents pairs on left hand side
v=1:size(I_leftside_set,2);
flag_similar_currents = 0;
if max(v)>=2
    C = nchoosek(v,2);
    record_pairs=[];

    for idx = 1:size(C,1)
        current_set_1 = I_leftside_set(:,C(idx,1));
        current_set_2 = I_leftside_set(:,C(idx,2));

        difference = abs(mean(current_set_1-current_set_2));

        if difference<=5
            record_pairs = [record_pairs; C(idx,:)];
            flag_similar_currents = 1;
        end
    end
end

% make right hand side currents negative because right side
I_rightside_set = (-1)*I_rightside_set;

% using lscov to estimate correction factors
if flag_similar_currents == 0  
    estimation_results = lscov(I_leftside_set,I_rightside_set);
else
    [U, S, V] = svd(I_leftside_set);
    r=rank(I_leftside_set);
    U1 = U(:,1:r);
    S1 = S(1:r,1:r);
    V1 = V(:,1:r);
    
    estimation_results = V1*(S1^(-1))*U1'*I_rightside_set;
end

if ~isempty(KI_injection_set)
    [left_lines,left_index] = setdiff(inaccurate_injection_lines_set(:,1),KI_injection_set(:,1),'stable');
    rest_KIs = [left_lines, estimation_results];
    KI_injection_set = [KI_injection_set; rest_KIs];
else
    KI_injection_set = [inaccurate_injection_lines_set(:,1), estimation_results];
end

end







